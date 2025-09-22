using HealthMonitor.Model.Analytics;
using HealthMonitor.Services;

namespace HealthMonitor.Grains;

public interface IHealthTrendAggregatorGrain : IGrainWithStringKey
{
    ValueTask<Dictionary<string, HealthTrendData>> GetAllServiceTrends(TimeSpan analysisWindow, CancellationToken token);
    ValueTask<HealthTrendComparisonReport> CompareServiceTrends(List<string> serviceIds, TimeSpan window, CancellationToken token);
    ValueTask<SystemHealthOverview> GetSystemOverview(CancellationToken token);
    ValueTask RefreshAllTrends(CancellationToken token);
}

public class HealthTrendAggregatorGrain(
    IServiceRegistry serviceRegistry,
    ILogger<HealthTrendAggregatorGrain> logger,
    IClusterClient clusterClient)
    : Grain, IHealthTrendAggregatorGrain
{
    public async ValueTask<Dictionary<string, HealthTrendData>> GetAllServiceTrends(TimeSpan analysisWindow, CancellationToken token)
    {
        // This would need to be configured with known service IDs or discovered dynamically
        var serviceIds = await GetKnownServiceIds(token);
        var trends = new Dictionary<string, HealthTrendData>();
        
        var tasks = serviceIds.Select(async serviceId =>
        {
            try
            {
                var trendGrain = clusterClient.GetGrain<IHealthTrendGrain>(serviceId);
                var trend = await trendGrain.CalculateTrend(analysisWindow, token);
                return new { ServiceId = serviceId, Trend = trend };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get trend for service {ServiceId}", serviceId);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results.Where(r => r != null))
        {
            trends[result!.ServiceId] = result.Trend;
        }

        return trends;
    }

    public async ValueTask<HealthTrendComparisonReport> CompareServiceTrends(List<string> serviceIds, TimeSpan window, CancellationToken token)
    {
        var trends = new List<ServiceTrendComparison>();
        
        foreach (var serviceId in serviceIds)
        {
            try
            {
                var trendGrain = clusterClient.GetGrain<IHealthTrendGrain>(serviceId);
                var trendData = await trendGrain.CalculateTrend(window, token);
                
                trends.Add(new ServiceTrendComparison
                {
                    ServiceId = serviceId,
                    TrendData = trendData,
                    RelativeHealthScore = trendData.OverallHealthScore
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get trend for service {ServiceId}", serviceId);
            }
        }

        // Rank services by health score
        trends = trends.OrderByDescending(t => t.TrendData.OverallHealthScore).ToList();
        for (int i = 0; i < trends.Count; i++)
        {
            trends[i].HealthRank = i + 1;
            trends[i].HealthInsights = GenerateHealthInsights(trends[i].TrendData);
        }

        return new HealthTrendComparisonReport
        {
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
        var serviceIds = await GetKnownServiceIds(token);
        var trends = await GetAllServiceTrends(TimeSpan.FromDays(1), token); // Last 24 hours
        
        var healthyServices = trends.Values.Count(t => t.OverallHealthScore >= 95);
        var problematicServices = trends.Values.Count(t => t.OverallHealthScore < 80);
        var overallHealth = trends.Values.Any() ? trends.Values.Average(t => t.OverallHealthScore) : 0;
        
        var alerts = new List<SystemHealthAlert>();
        foreach (var trend in trends.Values)
        {
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
            else if (trend.DetectedAnomalies.Any(a => a.Type == AnomalyType.LongOutage))
            {
                alerts.Add(new SystemHealthAlert
                {
                    ServiceId = trend.ServiceId,
                    Severity = AlertSeverity.Warning,
                    Message = "Recent service outages detected",
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        return new SystemHealthOverview
        {
            TotalServices = serviceIds.Count,
            HealthyServices = healthyServices,
            ProblematicServices = problematicServices,
            OverallSystemHealth = overallHealth,
            Alerts = alerts.OrderByDescending(a => a.Severity).ToList(),
            ServicesByTrend = trends.Values
                .GroupBy(t => t.HealthTrend.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async ValueTask RefreshAllTrends(CancellationToken token)
    {
        var serviceIds = await GetKnownServiceIds(token);
        
        var refreshTasks = serviceIds.Select(async serviceId =>
        {
            try
            {
                var trendGrain = clusterClient.GetGrain<IHealthTrendGrain>(serviceId);
                await trendGrain.RefreshTrendData(token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to refresh trend for service {ServiceId}", serviceId);
            }
        });

        await Task.WhenAll(refreshTasks);
        logger.LogInformation("Refreshed trend data for {Count} services", serviceIds.Count);
    }

    private async ValueTask<List<string>> GetKnownServiceIds(CancellationToken token)
    {
        var serviceIds = await serviceRegistry.GetAllServiceIds();
        return serviceIds.ToList();
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

