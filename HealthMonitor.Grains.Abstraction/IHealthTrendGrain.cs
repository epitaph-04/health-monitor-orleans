using HealthMonitor.Model;
using Orleans;

namespace HealthMonitor.Grains.Abstraction;

public interface IHealthTrendGrain : IGrainWithStringKey
{
    ValueTask<HealthTrendData> CalculateTrend(TimeSpan analysisWindow, CancellationToken token);
    ValueTask<HealthTrendData> GetLatestTrend(CancellationToken token);
    ValueTask<List<HealthTrendData>> GetTrendHistory(int count, CancellationToken token);
    ValueTask RefreshTrendData(CancellationToken token);
}