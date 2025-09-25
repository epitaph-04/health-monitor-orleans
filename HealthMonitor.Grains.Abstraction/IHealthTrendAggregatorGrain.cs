using HealthMonitor.Model;
using Orleans;

namespace HealthMonitor.Grains.Abstraction;

public interface IHealthTrendAggregatorGrain : IGrainWithStringKey
{
    ValueTask Initialize(HealthTrendsOptions option, CancellationToken token);
    ValueTask RegisterService(ServiceConfiguration serviceConfiguration, CancellationToken token);
    ValueTask<Dictionary<string, HealthTrendData>> GetAllServiceTrends(TimeSpan analysisWindow, CancellationToken token);
    ValueTask<HealthTrendComparisonReport> CompareServiceTrends(List<string> serviceIds, TimeSpan window, CancellationToken token);
    ValueTask<SystemHealthOverview> GetSystemOverview(CancellationToken token);
    ValueTask RefreshAllTrends(CancellationToken token);
}
