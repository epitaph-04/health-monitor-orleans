using HealthMonitor.Cluster.Services;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

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
}

[StorageProvider(ProviderName = "Default")]
public class HealthTrendGrain(
    ILogger<HealthTrendGrain> logger, 
    HealthTrendCalculator trendCalculator)
    : Grain<HealthTrendState>, IHealthTrendGrain
{
    public override async Task OnActivateAsync(CancellationToken token)
    {
        if (State.ServiceId.IsWhiteSpace())
        {
            State.ServiceId = this.GetPrimaryKeyString();
            State.TrendHistory = [];
        }
        await base.OnActivateAsync(token);
    }

    public async ValueTask<HealthTrendData> CalculateTrend(TimeSpan analysisWindow, CancellationToken token)
    {
        var serviceId = this.GetPrimaryKeyString();
        logger.LogInformation("Calculating trend for service {ServiceId} over {Window}", 
            serviceId, analysisWindow);

        try
        {
            // Get historical data from multiple time-partitioned grains
            var historicalData = await GetHistoricalData(analysisWindow, token);
            
            if (!historicalData.Any())
            {
                logger.LogWarning("No historical data found for service {ServiceId}", serviceId);
                return CreateEmptyTrend(serviceId, analysisWindow);
            }

            // Calculate comprehensive trend analysis
            var trendData = await trendCalculator.CalculateTrend(serviceId, historicalData, analysisWindow);
            
            // Store in trend history
            State.TrendHistory.Add(trendData);
            State.LastCalculated = DateTime.UtcNow;
            
            // Keep only recent trend history (last 100 calculations)
            if (State.TrendHistory.Count > 100)
            {
                State.TrendHistory = State.TrendHistory.TakeLast(100).ToList();
            }
            
            await WriteStateAsync();
            
            logger.LogInformation("Calculated trend for {ServiceId}: Health={Health:F2}%, Availability={Availability:F2}%, Trend={Trend}",
                serviceId, trendData.OverallHealthScore, trendData.AvailabilityPercentage, trendData.HealthTrend);

            return trendData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate trend for service {ServiceId}", serviceId);
            throw;
        }
    }

    public ValueTask<HealthTrendData> GetLatestTrend(CancellationToken token)
    {
        var latestTrend = State.TrendHistory.OrderByDescending(t => t.CalculatedAt).FirstOrDefault();
        return ValueTask.FromResult(latestTrend ?? CreateEmptyTrend(State.ServiceId, TimeSpan.FromDays(1)));
    }

    public ValueTask<List<HealthTrendData>> GetTrendHistory(int count, CancellationToken token)
    {
        var history = State.TrendHistory
            .OrderByDescending(t => t.CalculatedAt)
            .Take(count)
            .ToList();
        
        return ValueTask.FromResult(history);
    }

    public async ValueTask RefreshTrendData(CancellationToken token)
    {
        // Refresh trend data with different time windows
        var windows = new[]
        {
            TimeSpan.FromHours(24),   // Last 24 hours
            TimeSpan.FromDays(7),     // Last week
            TimeSpan.FromDays(30),    // Last month
            TimeSpan.FromDays(90)     // Last 3 months
        };

        foreach (var window in windows)
        {
            await CalculateTrend(window, token);
        }
    }

    private async ValueTask<List<HealthCheckRecord>> GetHistoricalData(TimeSpan window, CancellationToken token)
    {
        var serviceId = this.GetPrimaryKeyString();
        var fromTime = DateTime.UtcNow - window;
        var toTime = DateTime.UtcNow;

        // Get data from multiple time-partitioned grains
        var allRecords = new List<HealthCheckRecord>();

        // For 3 months of data, we might need to query multiple grains
        // Each grain could store data for a specific time period (e.g., daily/weekly grains)
        //var timePartitions = GetTimePartitions(fromTime, toTime);
        
        //foreach (var partition in timePartitions)
        //{
            try
            {
                //var grainKey = $"{serviceId}-{partition:yyyy-MM-dd}";
                var grainKey = serviceId;
                var dataGrain = GrainFactory.GetGrain<IHealthCheckGrain>(grainKey);
                var records = await dataGrain.GetHealthRecords(fromTime, toTime, token);
                allRecords.AddRange(records);
            }
            catch (Exception ex)
            {
                //logger.LogWarning(ex, "Failed to get data from partition {Partition} for service {ServiceId}", 
                //    partition, serviceId);
                logger.LogWarning(ex, "Failed to get data for service {ServiceId}", 
                    serviceId);
            }
        //}

        return allRecords.OrderBy(r => r.Timestamp).ToList();
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
}