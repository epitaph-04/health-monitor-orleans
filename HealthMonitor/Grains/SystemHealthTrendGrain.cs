using HealthMonitor.Model.Analytics;
using HealthMonitor.Services;

namespace HealthMonitor.Grains;

public interface ISystemHealthTrendGrain : IGrainWithGuidKey
{
    ValueTask<SystemHealthTrend> GenerateOverallHealthTrend(TimeSpan period, CancellationToken token);
}

public class SystemHealthTrendGrain(
    IClusterClient client,
    IServiceRegistry serviceRegistry,
    ILogger<SystemHealthTrendGrain> logger)
    : Grain, ISystemHealthTrendGrain
{
    public async ValueTask<SystemHealthTrend> GenerateOverallHealthTrend(TimeSpan period, CancellationToken token)
    {
        var serviceIds = (await serviceRegistry.GetAllServiceIds()).ToList();
        if (serviceIds.Count == 0)
        {
            logger.LogWarning("No services found in the registry. Cannot generate health trend.");
            return new SystemHealthTrend { Period = period, AggregatedDataPoints = new List<HealthDataPoint>() };
        }
        
        var trendTasks = serviceIds.Select(id => 
            client.GetGrain<IServiceHealthTrendGrain>(id).CalculateTrend(period, token).AsTask()
        );

        var individualResults = await Task.WhenAll(trendTasks);

        // 2. Identify top improving and degrading services from the results.
        var topImproving = individualResults
            .Where(r => r.Direction == TrendDirection.Increasing)
            .OrderByDescending(r => r.Strength)
            .Take(5)
            .ToList();

        var topDegrading = individualResults
            .Where(r => r.Direction == TrendDirection.Decreasing)
            .OrderBy(r => r.Strength)
            .Take(5)
            .ToList();
            
        // 3. Fetch all data points to calculate aggregate scores for the system-wide chart.
        // This could be further optimized in a real system (e.g., with a dedicated analytics store).
        var dataPointTasks = serviceIds.Select(id => 
            client.GetGrain<IHealthCheckGrain>(id).GetHistoricalCheckResults(DateTime.UtcNow - period, DateTime.UtcNow, token).AsTask()
        ).ToList();

        var allDataPointsNested = await Task.WhenAll(dataPointTasks);
        var allDataPointsFlat = allDataPointsNested.SelectMany(
            list => list.Select(d => new HealthDataPoint(d.CheckedTimeUtc, d.Status.ToScore()))
            ).ToList();

        // 4. Aggregate data points into system-wide intervals (e.g., per minute)
        var aggregatedData = allDataPointsFlat
            .GroupBy(p => new DateTime(p.Timestamp.Year, p.Timestamp.Month, p.Timestamp.Day, p.Timestamp.Hour, p.Timestamp.Minute, 0))
            .Select(g => new HealthDataPoint(g.Key, g.Average(p => p.HealthScore)))
            .OrderBy(p => p.Timestamp)
            .ToList();
            
        // 5. Calculate final aggregate metrics
        var currentScore = aggregatedData.LastOrDefault()?.HealthScore ?? 0;
        var averageScore = aggregatedData.Any() ? aggregatedData.Average(p => p.HealthScore) : 0;
        
        // The overall trend is simply the trend of the aggregated data
        var overallTrendGrain = client.GetGrain<IServiceHealthTrendGrain>("__SYSTEM_AGGREGATE__");
        var systemTrendResult = await CalculateAggregateTrend(aggregatedData, period);


        return new SystemHealthTrend
        {
            Period = period,
            CurrentOverallHealthScore = currentScore,
            AverageOverallHealthScore = averageScore,
            OverallTrend = systemTrendResult.Direction,
            OverallTrendStrength = systemTrendResult.Strength,
            AggregatedDataPoints = aggregatedData,
            TopImprovingServices = topImproving,
            TopDegradingServices = topDegrading
        };
    }

    // This is a helper to reuse the same linear regression logic for the aggregated data
    private ValueTask<ServiceTrendResult> CalculateAggregateTrend(List<HealthDataPoint> aggregatedData, TimeSpan period)
    {
        // We can re-use the logic from ServiceHealthTrendGrain.
        // A more advanced implementation might have this logic in a shared library.
        // For now, we'll just call the same private calculation method (conceptually).
        // This is a simplified version of the logic in ServiceHealthTrendGrain.
         var (slope, _) = ServiceHealthTrendGrainUtils.CalculateLinearRegression(aggregatedData);
         const double stableThreshold = 0.001; 
         TrendDirection direction = slope > stableThreshold ? TrendDirection.Increasing : slope < -stableThreshold ? TrendDirection.Decreasing : TrendDirection.Stable;
         return ValueTask.FromResult(new ServiceTrendResult("__SYSTEM_AGGREGATE__", direction, slope));
    }
}

// Helper class to expose the internal calculation method for the aggregator grain
public static class ServiceHealthTrendGrainUtils
{
    public static (double slope, double intercept) CalculateLinearRegression(List<HealthDataPoint> points)
    {
        // ... (The exact same implementation as the private method in the real grain)
        int n = points.Count;
        if (n < 2) return (0, 0);
        long firstTimestampTicks = points[0].Timestamp.Ticks;
        double sumX = 0, sumY = 0, sumXy = 0, sumX2 = 0;
        foreach (var p in points)
        {
            double x = p.Timestamp.Ticks - firstTimestampTicks;
            double y = p.HealthScore;
            sumX += x; sumY += y; sumXy += x * y; sumX2 += x * x;
        }
        double slope = (n * sumXy - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;
        return (slope, intercept);
    }
}