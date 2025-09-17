using HealthMonitor.Model.Analytics;

namespace HealthMonitor.Services;

public interface IAnalyticsService
{
    public ValueTask<SystemHealthTrend> GetSystemHealthTrend(TimeSpan period, CancellationToken token);
}