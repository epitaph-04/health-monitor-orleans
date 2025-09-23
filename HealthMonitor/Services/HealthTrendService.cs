using HealthMonitor.Grains;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;

namespace HealthMonitor.Services;

public class HealthTrendService(IGrainFactory grainFactory, ILogger<HealthTrendService> logger) : IHealthTrendService
{
    public async Task<HealthTrendData> GetServiceTrend(string serviceId, int hours, CancellationToken token)
    {
        var trendGrain = grainFactory.GetGrain<IHealthTrendGrain>(serviceId);
        var trend = await trendGrain.CalculateTrend(TimeSpan.FromHours(hours), token);
        return trend;
    }

    public async Task<List<HealthTrendData>> GetServiceTrendHistory(string serviceId, int count, CancellationToken token)
    {
        var trendGrain = grainFactory.GetGrain<IHealthTrendGrain>(serviceId);
        var history = await trendGrain.GetTrendHistory(count, token);
        return history;
    }

    public async Task<SystemHealthOverview> GetSystemOverview(CancellationToken token)
    {
        var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");
        var overview = await aggregatorGrain.GetSystemOverview(token);
        return overview;
    }

    public async Task<HealthTrendComparisonReport> CompareServices(CompareServicesRequest request, CancellationToken token)
    {
        var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");
        var comparison = await aggregatorGrain.CompareServiceTrends(
            request.ServiceIds, 
            TimeSpan.FromHours(request.Hours), token);
        return comparison;
    }
    
    public async Task RefreshAllTrends(CancellationToken token)
    {
        var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");
        await aggregatorGrain.RefreshAllTrends(token);
    }
}