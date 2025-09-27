using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Orleans.Providers;

namespace HealthMonitor.Cluster.Grains;

[GenerateSerializer]
public class HealthTrendAggregatorState
{
    [Id(0)]
    public HashSet<string> ServiceIds { get; set; } = [];
    [Id(1)]
    public DateTime LastRefresh { get; set; }
    [Id(2)]
    public SystemHealthOverview? CachedOverview { get; set; }
    [Id(3)]
    public DateTime OverviewCacheExpiry { get; set; }
}

[StorageProvider(ProviderName = "Default")]
public class HealthTrendAggregatorGrain(
    IClusterClient clusterClient,
    ILogger<HealthTrendAggregatorGrain> logger)
    : Grain<HealthTrendAggregatorState>, IHealthTrendAggregatorGrain, IRemindable
{
    private const string HealthTrendAggregatorReminder = "HEALTH_TREND_AGGREGATOR_REMINDER";
    public async ValueTask Initialize(HealthTrendsOptions options, CancellationToken token)
    {
        await this.RegisterOrUpdateReminder(
            $"{HealthTrendAggregatorReminder}-{this.GetPrimaryKeyString()}", 
            TimeSpan.FromSeconds(30),
            options.CalculationInterval);
    }

    public async ValueTask RegisterService(ServiceConfiguration serviceConfiguration, CancellationToken token)
    {
        State.ServiceIds.Add(serviceConfiguration.Id);
        await WriteStateAsync();
        await clusterClient.GetGrain<IHealthCheckGrain>(serviceConfiguration.Id).Register(serviceConfiguration, token);
    }

    public async ValueTask<Dictionary<string, HealthTrendData>> GetAllServiceTrends(TimeSpan analysisWindow, CancellationToken token)
    {
        var trends = new Dictionary<string, HealthTrendData>();

        if (!State.ServiceIds.Any())
        {
            logger.LogWarning("No services registered in aggregator");
            return trends;
        }

        // Use semaphore to limit concurrent grain calls
        using var semaphore = new SemaphoreSlim(5); // Max 5 concurrent calls

        var tasks = State.ServiceIds.Select(async serviceId =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                var trendGrain = clusterClient.GetGrain<IHealthTrendGrain>(serviceId);

                // Use GetLatestTrend for better performance, fallback to calculation if needed
                var latestTrend = await trendGrain.GetLatestTrend(token);

                // If trend is too old or empty, calculate new one
                if (latestTrend == null ||
                    DateTime.UtcNow - latestTrend.CalculatedAt > TimeSpan.FromMinutes(10) ||
                    latestTrend.TimeWindow != analysisWindow)
                {
                    latestTrend = await trendGrain.CalculateTrend(analysisWindow, token);
                }

                return new { ServiceId = serviceId, Trend = latestTrend };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get trend for service {ServiceId}", serviceId);
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        foreach (var result in results.Where(r => r != null))
        {
            trends[result!.ServiceId] = result.Trend;
        }

        logger.LogInformation("Retrieved trends for {Count}/{Total} services",
            trends.Count, State.ServiceIds.Count);

        return trends;
    }

    public async ValueTask<HealthTrendComparisonReport> CompareServiceTrends(List<string> serviceIds, TimeSpan window, CancellationToken token)
    {
        var trends = new List<ServiceTrendComparison>();

        // Filter to only include registered services
        var validServiceIds = serviceIds.Where(id => State.ServiceIds.Contains(id)).ToList();

        if (!validServiceIds.Any())
        {
            logger.LogWarning("No valid service IDs provided for comparison");
            return new HealthTrendComparisonReport
            {
                AnalysisWindow = window,
                ServiceComparisons = trends,
                HealthRanking = new SystemHealthRanking()
            };
        }

        // Use semaphore to limit concurrent calls
        using var semaphore = new SemaphoreSlim(3);

        var tasks = validServiceIds.Select(async serviceId =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                var trendGrain = clusterClient.GetGrain<IHealthTrendGrain>(serviceId);
                var trendData = await trendGrain.GetLatestTrend(token);

                // Calculate new trend if needed
                if (trendData == null || DateTime.UtcNow - trendData.CalculatedAt > TimeSpan.FromMinutes(5))
                {
                    trendData = await trendGrain.CalculateTrend(window, token);
                }

                return new ServiceTrendComparison
                {
                    ServiceId = serviceId,
                    TrendData = trendData,
                    RelativeHealthScore = trendData.OverallHealthScore,
                    HealthInsights = GenerateHealthInsights(trendData)
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get trend for service {ServiceId}", serviceId);
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        trends = results.Where(r => r != null).ToList()!;

        // Rank services by health score
        trends = trends.OrderByDescending(t => t.TrendData.OverallHealthScore).ToList();
        for (int i = 0; i < trends.Count; i++)
        {
            trends[i].HealthRank = i + 1;
        }

        return new HealthTrendComparisonReport
        {
            GeneratedAt = DateTime.UtcNow,
            AnalysisWindow = window,
            ServiceComparisons = trends,
            HealthRanking = new SystemHealthRanking
            {
                HealthiestServices = trends.Take(3).Select(t => t.ServiceId).ToList(),
                ProblematicServices = trends.Where(t => t.TrendData.OverallHealthScore < 80).Select(t => t.ServiceId).ToList(),
                ImprovingServices = trends.Where(t => t.TrendData.HealthTrend == HealthTrendDirection.Improving).Select(t => t.ServiceId).ToList(),
                DecliningServices = trends.Where(t => t.TrendData.HealthTrend == HealthTrendDirection.Declining).Select(t => t.ServiceId).ToList()
            }
        };
    }

    public async ValueTask<SystemHealthOverview> GetSystemOverview(CancellationToken token)
    {
        // Check cache first
        if (State.CachedOverview != null && DateTime.UtcNow < State.OverviewCacheExpiry)
        {
            logger.LogDebug("Returning cached system overview");
            return State.CachedOverview;
        }

        logger.LogInformation("Generating system health overview for {ServiceCount} services", State.ServiceIds.Count);

        var trends = await GetAllServiceTrends(TimeSpan.FromHours(24), token); // Last 24 hours

        if (!trends.Any())
        {
            logger.LogWarning("No trend data available for system overview");
            return new SystemHealthOverview
            {
                TotalServices = State.ServiceIds.Count,
                GeneratedAt = DateTime.UtcNow
            };
        }

        var healthyServices = trends.Values.Count(t => t.OverallHealthScore >= 95);
        var problematicServices = trends.Values.Count(t => t.OverallHealthScore < 80);
        var overallHealth = trends.Values.Average(t => t.OverallHealthScore);

        var alerts = GenerateSystemAlerts(trends.Values);

        var overview = new SystemHealthOverview
        {
            GeneratedAt = DateTime.UtcNow,
            TotalServices = State.ServiceIds.Count,
            HealthyServices = healthyServices,
            ProblematicServices = problematicServices,
            OverallSystemHealth = overallHealth,
            Alerts = alerts.OrderByDescending(a => a.Severity).ThenByDescending(a => a.DetectedAt).ToList(),
            ServicesByTrend = trends.Values
                .GroupBy(t => t.HealthTrend.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Cache the result
        State.CachedOverview = overview;
        State.OverviewCacheExpiry = DateTime.UtcNow.AddMinutes(2); // Cache for 2 minutes
        await WriteStateAsync();

        logger.LogInformation("Generated system overview: {HealthyServices}/{TotalServices} healthy, overall health {OverallHealth:F1}%",
            healthyServices, State.ServiceIds.Count, overallHealth);

        return overview;
    }

    private List<SystemHealthAlert> GenerateSystemAlerts(IEnumerable<HealthTrendData> trends)
    {
        var alerts = new List<SystemHealthAlert>();

        foreach (var trend in trends)
        {
            // Critical health alerts
            if (trend.OverallHealthScore < 70)
            {
                alerts.Add(new SystemHealthAlert
                {
                    ServiceId = trend.ServiceId,
                    Severity = AlertSeverity.Critical,
                    Message = $"Service health critically low: {trend.OverallHealthScore:F1}%",
                    DetectedAt = DateTime.UtcNow
                });
            }
            // Warning for declining services
            else if (trend.OverallHealthScore < 90 && trend.HealthTrend == HealthTrendDirection.Declining)
            {
                alerts.Add(new SystemHealthAlert
                {
                    ServiceId = trend.ServiceId,
                    Severity = AlertSeverity.Warning,
                    Message = $"Service health declining: {trend.OverallHealthScore:F1}%",
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Anomaly-based alerts
            foreach (var anomaly in trend.DetectedAnomalies.Where(a => a.Severity > 0.7))
            {
                var severity = anomaly.Type == AnomalyType.LongOutage ? AlertSeverity.Critical : AlertSeverity.Warning;

                alerts.Add(new SystemHealthAlert
                {
                    ServiceId = trend.ServiceId,
                    Severity = severity,
                    Message = anomaly.Description,
                    DetectedAt = anomaly.StartTime
                });
            }
        }

        return alerts;
    }

    public async ValueTask RefreshAllTrends(CancellationToken token)
    {
        if (!State.ServiceIds.Any())
        {
            logger.LogInformation("No services registered for trend refresh");
            return;
        }

        logger.LogInformation("Starting trend refresh for {Count} services", State.ServiceIds.Count);

        // Clear cached overview to force regeneration
        State.CachedOverview = null;
        State.OverviewCacheExpiry = DateTime.MinValue;

        // Use semaphore to control concurrency
        using var semaphore = new SemaphoreSlim(3);

        var refreshTasks = State.ServiceIds.Select(async serviceId =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                var trendGrain = clusterClient.GetGrain<IHealthTrendGrain>(serviceId);
                await trendGrain.RefreshTrendData(token);
                logger.LogDebug("Refreshed trend data for service {ServiceId}", serviceId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to refresh trend for service {ServiceId}", serviceId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(refreshTasks);

        State.LastRefresh = DateTime.UtcNow;
        await WriteStateAsync();

        logger.LogInformation("Completed trend refresh for {Count} services", State.ServiceIds.Count);
    }
    
    async Task IRemindable.ReceiveReminder(string reminderName, Orleans.Runtime.TickStatus status)
    {
        if (reminderName.StartsWith(HealthTrendAggregatorReminder))
        {
            try
            {
                await RefreshAllTrends(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh trends in reminder");
            }
        }
    }

    private List<string> GenerateHealthInsights(HealthTrendData trendData)
    {
        var insights = new List<string>();

        if (trendData.AvailabilityPercentage < 99)
        {
            insights.Add($"Availability below 99% ({trendData.AvailabilityPercentage:F2}%)");
        }

        if (trendData.AverageResponseTime.TotalSeconds > 2)
        {
            insights.Add($"High average response time ({trendData.AverageResponseTime.TotalSeconds:F1}s)");
        }

        if (trendData.DetectedAnomalies.Any(a => a.Type == AnomalyType.LongOutage))
        {
            var outageCount = trendData.DetectedAnomalies.Count(a => a.Type == AnomalyType.LongOutage);
            insights.Add($"{outageCount} service outages detected");
        }

        if (trendData.HealthTrend == HealthTrendDirection.Improving)
        {
            insights.Add("Health trend is improving");
        }
        else if (trendData.HealthTrend == HealthTrendDirection.Declining)
        {
            insights.Add("Health trend is declining - needs attention");
        }

        if (!trendData.SlaMetrics.MeetingAvailabilitySla)
        {
            insights.Add("Not meeting availability SLA");
        }

        if (!trendData.SlaMetrics.MeetingResponseTimeSla)
        {
            insights.Add("Not meeting response time SLA");
        }

        return insights;
    }
}

