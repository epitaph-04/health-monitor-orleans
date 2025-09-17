using HealthMonitor.Model;
using HealthMonitor.Model.Analytics;
using TrendDirection = HealthMonitor.Model.Analytics.TrendDirection;

namespace HealthMonitor.Grains;

public interface IServiceHealthTrendGrain : IGrainWithStringKey
{
    ValueTask<ServiceTrendResult> CalculateTrend(TimeSpan period, CancellationToken token);
}

public class ServiceHealthTrendGrain(IClusterClient client, ILogger<ServiceHealthTrendGrain> logger)
    : Grain, IServiceHealthTrendGrain
{
    public async ValueTask<ServiceTrendResult> CalculateTrend(TimeSpan period, CancellationToken token)
    {
        var serviceId = this.GetPrimaryKeyString();
        var endDate = DateTime.UtcNow;
        var startDate = endDate - period;

        // 1. Get historical data from the corresponding HealthCheckGrain
        var healthCheckGrain = client.GetGrain<IHealthCheckGrain>(serviceId);
        var dataPoints = await healthCheckGrain.GetHistoricalCheckResults(startDate, endDate, token);

        if (dataPoints.Count < 2)
        {
            // Cannot determine a trend without at least two data points
            return new ServiceTrendResult(serviceId, TrendDirection.Stable, 0);
        }

        // 2. Perform Linear Regression to find the trend line
        var (slope, _) = CalculateLinearRegression(
            dataPoints
                .Select(d => new HealthDataPoint(d.CheckedTimeUtc, d.Status.ToScore()))
                .ToList());

        // 3. Determine direction and strength from the slope
        // Slope represents the rate of change of health score over time.
        const double stableThreshold = 0.001; // A small threshold to ignore minor fluctuations
        TrendDirection direction;
        if (slope > stableThreshold)
        {
            direction = TrendDirection.Increasing;
        }
        else if (slope < -stableThreshold)
        {
            direction = TrendDirection.Decreasing;
        }
        else
        {
            direction = TrendDirection.Stable;
        }

        logger.LogInformation("Calculated trend for service {ServiceId}. Slope: {Slope}, Direction: {Direction}", serviceId, slope, direction);

        return new ServiceTrendResult(serviceId, direction, slope);
    }

    /// <summary>
    /// Calculates the slope and intercept of the best-fit line for the given data points.
    /// </summary>
    /// <returns>A tuple containing (slope, intercept).</returns>
    private static (double slope, double intercept) CalculateLinearRegression(List<HealthDataPoint> points)
    {
        int n = points.Count;
        if (n < 2) return (0, 0);

        // Using ticks for x-values provides high precision for the time component.
        // We subtract the first point's ticks to keep the numbers smaller and prevent overflow.
        long firstTimestampTicks = points[0].Timestamp.Ticks;

        double sumX = 0, sumY = 0, sumXy = 0, sumX2 = 0;

        foreach (var p in points)
        {
            double x = p.Timestamp.Ticks - firstTimestampTicks;
            double y = p.HealthScore;

            sumX += x;
            sumY += y;
            sumXy += x * y;
            sumX2 += x * x;
        }

        // Standard linear regression formula for the slope (m)
        double slope = (n * sumXy - sumX * sumY) / (n * sumX2 - sumX * sumX);

        // Formula for the y-intercept (b)
        double intercept = (sumY - slope * sumX) / n;

        return (slope, intercept);
    }
}

public static class HealthStatusToScore
{
    public static double ToScore(this Status status) =>
        status switch
        {
            Status.Healthy => 100.0,
            _ => 0.0,
        };
}