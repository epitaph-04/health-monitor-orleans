using HealthMonitor.Cluster.Services;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Orleans.Providers;
using Orleans.Timers;

namespace HealthMonitor.Cluster.Grains;

[GenerateSerializer]
public class HealthTrendState
{
    [Id(0)]
    public string ServiceId { get; set; } = "";
    [Id(1)]
    public List<HealthTrendData> TrendHistory { get; set; } = new();
    [Id(2)]
    public DateTime LastCalculated { get; set; }
    [Id(3)]
    public Dictionary<string, HealthTrendData> CachedTrends { get; set; } = new();
    [Id(4)]
    public DateTime LastCacheRefresh { get; set; }
}

[StorageProvider(ProviderName = "Default")]
public class HealthTrendGrain(
    ILogger<HealthTrendGrain> logger,
    HealthTrendCalculator trendCalculator)
    : Grain<HealthTrendState>, IHealthTrendGrain, IRemindable
{
    public override async Task OnActivateAsync(CancellationToken token)
    {
        if (State.ServiceId.IsWhiteSpace())
        {
            State.ServiceId = this.GetPrimaryKeyString();
            State.TrendHistory = [];
            State.CachedTrends = new Dictionary<string, HealthTrendData>();
        }

        // Set up automatic cache refresh
        await this.RegisterOrUpdateReminder(
            $"TrendRefresh-{this.GetPrimaryKeyString()}",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(10)); // Refresh every 10 minutes

        await base.OnActivateAsync(token);
    }

    public async ValueTask<HealthTrendData> CalculateTrend(TimeSpan analysisWindow, CancellationToken token)
    {
        var serviceId = this.GetPrimaryKeyString();
        var cacheKey = $"{analysisWindow.TotalHours}h";

        // Check grain storage cache first - this is fast since grain is already in memory
        if (State.CachedTrends.TryGetValue(cacheKey, out var cachedTrend))
        {
            // Use cached data if it's fresh enough (5 minutes for most requests, 1 minute for real-time dashboard)
            var cacheValidityWindow = analysisWindow <= TimeSpan.FromHours(1) ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(5);

            if (DateTime.UtcNow - cachedTrend.CalculatedAt < cacheValidityWindow)
            {
                logger.LogDebug("Returning grain-cached trend for service {ServiceId} window {Window} (cached {Age:F1}min ago)",
                    serviceId, analysisWindow, (DateTime.UtcNow - cachedTrend.CalculatedAt).TotalMinutes);
                return cachedTrend;
            }
        }

        logger.LogInformation("Calculating fresh trend for service {ServiceId} over {Window}",
            serviceId, analysisWindow);

        try
        {
            // Get historical data with retry logic
            var historicalData = await GetHistoricalDataWithRetry(analysisWindow, token);

            if (!historicalData.Any())
            {
                logger.LogWarning("No historical data found for service {ServiceId}", serviceId);
                var emptyTrend = CreateEmptyTrend(serviceId, analysisWindow);

                // Cache empty result but for shorter time to avoid repeated empty responses
                State.CachedTrends[cacheKey] = emptyTrend;
                await WriteStateAsync();
                return emptyTrend;
            }

            // Calculate comprehensive trend analysis using both current data and trend history
            var trendData = await trendCalculator.CalculateTrend(serviceId, historicalData, analysisWindow, State.TrendHistory);

            // Store in grain storage cache - this persists across grain activations
            State.CachedTrends[cacheKey] = trendData;

            // Add to trend history for prediction and confidence calculation
            // This is different from caching - it's for machine learning and trend analysis
            var shouldAddToHistory = ShouldAddToTrendHistory(trendData);

            if (shouldAddToHistory)
            {
                State.TrendHistory.Add(trendData);
                State.LastCalculated = DateTime.UtcNow;

                // Keep trend history for predictions (last 200 data points for better ML)
                // This gives us ~2-3 months of data for prediction algorithms
                if (State.TrendHistory.Count > 200)
                {
                    State.TrendHistory = State.TrendHistory
                        .OrderByDescending(t => t.CalculatedAt)
                        .Take(200)
                        .ToList();
                }

                logger.LogDebug("Added trend to history for service {ServiceId}. History size: {Count}",
                    this.GetPrimaryKeyString(), State.TrendHistory.Count);
            }

            // Cleanup old cached trends to prevent state bloat
            CleanupOldCachedTrends();

            await WriteStateAsync();

            logger.LogInformation("Calculated and cached trend for {ServiceId}: Health={Health:F2}%, Availability={Availability:F2}%, Trend={Trend}",
                serviceId, trendData.OverallHealthScore, trendData.AvailabilityPercentage, trendData.HealthTrend);

            return trendData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate trend for service {ServiceId}", serviceId);
            return CreateEmptyTrend(serviceId, analysisWindow);
        }
    }

    public ValueTask<HealthTrendData> GetLatestTrend(CancellationToken token)
    {
        var latestTrend = State.TrendHistory.OrderByDescending(t => t.CalculatedAt).FirstOrDefault();
        return ValueTask.FromResult(latestTrend ?? CreateEmptyTrend(State.ServiceId, TimeSpan.FromDays(1)));
    }

    public ValueTask<List<HealthTrendData>> GetTrendHistory(int count, CancellationToken token)
    {
        var history = State
            .TrendHistory
            .OrderByDescending(t => t.CalculatedAt)
            .Take(count)
            .ToList();
        
        return ValueTask.FromResult(history);
    }

    public async ValueTask RefreshTrendData(CancellationToken token)
    {
        logger.LogInformation("Refreshing trend data for service {ServiceId}", this.GetPrimaryKeyString());

        // Clear grain storage cache to force fresh calculations
        State.CachedTrends.Clear();
        State.LastCacheRefresh = DateTime.UtcNow;

        // Refresh common time windows that the dashboard uses
        var windows = new[]
        {
            TimeSpan.FromHours(1),    // Last hour (for real-time monitoring)
            TimeSpan.FromHours(24),   // Last 24 hours (most common)
            TimeSpan.FromDays(7),     // Last week
            TimeSpan.FromDays(30)     // Last month
        };

        // Calculate trends for all windows and store in grain storage
        var refreshTasks = windows.Select(async window => await CalculateTrend(window, token));
        await Task.WhenAll(refreshTasks);

        logger.LogInformation("Completed trend refresh for service {ServiceId} - cached {Count} trend calculations",
            this.GetPrimaryKeyString(), State.CachedTrends.Count);
    }

    private async ValueTask<List<HealthCheckRecord>> GetHistoricalDataWithRetry(TimeSpan window, CancellationToken token)
    {
        var serviceId = this.GetPrimaryKeyString();
        var fromTime = DateTime.UtcNow - window;
        var toTime = DateTime.UtcNow;

        const int maxRetries = 3;
        var attempts = 0;

        while (attempts < maxRetries)
        {
            try
            {
                var dataGrain = GrainFactory.GetGrain<IHealthCheckGrain>(serviceId);
                var records = await dataGrain.GetHealthRecords(fromTime, toTime, token);

                if (records.Any())
                {
                    logger.LogDebug("Retrieved {Count} records for service {ServiceId}",
                        records.Count, serviceId);
                    return records.OrderBy(r => r.Timestamp).ToList();
                }

                // If no records found, try with a larger window for recent services
                if (window < TimeSpan.FromDays(7))
                {
                    var extendedRecords = await dataGrain.GetRecentRecords(TimeSpan.FromDays(1), token);
                    return extendedRecords.OrderBy(r => r.Timestamp).ToList();
                }

                return new List<HealthCheckRecord>();
            }
            catch (Exception ex)
            {
                attempts++;
                logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed to get data for service {ServiceId}",
                    attempts, maxRetries, serviceId);

                if (attempts >= maxRetries)
                {
                    logger.LogError(ex, "All {MaxRetries} attempts failed to get data for service {ServiceId}",
                        maxRetries, serviceId);
                    return new List<HealthCheckRecord>();
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempts) * 100), token);
            }
        }

        return new List<HealthCheckRecord>();
    }

    private List<DateTime> GetTimePartitions(DateTime fromTime, DateTime toTime)
    {
        var partitions = new List<DateTime>();
        var current = fromTime.Date;
        
        while (current <= toTime.Date)
        {
            partitions.Add(current);
            current = current.AddDays(1);
        }
        
        return partitions;
    }

    private HealthTrendData CreateEmptyTrend(string serviceId, TimeSpan window)
    {
        return new HealthTrendData
        {
            ServiceId = serviceId,
            CalculatedAt = DateTime.UtcNow,
            TimeWindow = window,
            HealthTrend = HealthTrendDirection.Unknown,
            ResponseTimeTrend = HealthTrendDirection.Unknown
        };
    }

    private bool ShouldAddToTrendHistory(HealthTrendData trendData)
    {
        // Add to trend history for prediction purposes if:
        // 1. First calculation ever
        // 2. Significant time has passed (for temporal patterns)
        // 3. Significant change in health score (for anomaly detection)
        // 4. Different time window than last entry (for pattern analysis)

        if (State.TrendHistory.Count == 0)
            return true;

        var lastTrend = State.TrendHistory.OrderByDescending(t => t.CalculatedAt).First();

        // Add if 30 minutes have passed (for temporal pattern analysis)
        if (DateTime.UtcNow - lastTrend.CalculatedAt > TimeSpan.FromMinutes(30))
            return true;

        // Add if significant health change (>5%) for anomaly detection
        if (Math.Abs(trendData.OverallHealthScore - lastTrend.OverallHealthScore) > 5.0)
            return true;

        // Add if different analysis window (for multi-timeframe analysis)
        if (trendData.TimeWindow != lastTrend.TimeWindow)
            return true;

        // Add if trend direction changed (important for prediction)
        if (trendData.HealthTrend != lastTrend.HealthTrend)
            return true;

        return false;
    }

    private void CleanupOldCachedTrends()
    {
        // Remove cached trends older than 1 hour to prevent grain state bloat
        // Note: This is separate from TrendHistory which is kept longer for predictions
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        var keysToRemove = State.CachedTrends
            .Where(kvp => kvp.Value.CalculatedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            State.CachedTrends.Remove(key);
        }

        if (keysToRemove.Any())
        {
            logger.LogDebug("Cleaned up {Count} old cached trends for service {ServiceId}",
                keysToRemove.Count, this.GetPrimaryKeyString());
        }
    }

    async Task IRemindable.ReceiveReminder(string reminderName, Orleans.Runtime.TickStatus status)
    {
        if (reminderName.StartsWith("TrendRefresh"))
        {
            try
            {
                await RefreshTrendData(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh trend data in reminder for service {ServiceId}",
                    this.GetPrimaryKeyString());
            }
        }
    }
}