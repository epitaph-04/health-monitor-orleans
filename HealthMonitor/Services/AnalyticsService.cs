using HealthMonitor.Grains;
using HealthMonitor.Model.Analytics;

namespace HealthMonitor.Services;

public class AnalyticsService(IClusterClient client) : IAnalyticsService
{
    public async ValueTask<SystemHealthTrend> GetSystemHealthTrend(TimeSpan period, CancellationToken token)
        => await client.GetGrain<ISystemHealthTrendGrain>(Guid.Empty).GenerateOverallHealthTrend(period, token);
}