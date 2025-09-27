using HealthMonitor.Grains;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;

namespace HealthMonitor.Services;

public class HealthTrendService(
    IGrainFactory grainFactory,
    ILogger<HealthTrendService> logger) : IHealthTrendService
{
    public async Task<HealthTrendData> GetServiceTrend(string serviceId, int hours, CancellationToken token)
    {
        try
        {
            var trendGrain = grainFactory.GetGrain<IHealthTrendGrain>(serviceId);

            // Let the grain handle caching internally - it will return cached data if fresh enough
            var trend = await trendGrain.CalculateTrend(TimeSpan.FromHours(hours), token);

            return trend;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get service trend for {ServiceId}", serviceId);

            // Return fallback empty trend
            return new HealthTrendData
            {
                ServiceId = serviceId,
                CalculatedAt = DateTime.UtcNow,
                TimeWindow = TimeSpan.FromHours(hours),
                HealthTrend = HealthTrendDirection.Unknown,
                ResponseTimeTrend = HealthTrendDirection.Unknown
            };
        }
    }

    public async Task<List<HealthTrendData>> GetServiceTrendHistory(string serviceId, int count, CancellationToken token)
    {
        try
        {
            var trendGrain = grainFactory.GetGrain<IHealthTrendGrain>(serviceId);
            var history = await trendGrain.GetTrendHistory(count, token);

            return history;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get service trend history for {ServiceId}", serviceId);
            return new List<HealthTrendData>();
        }
    }

    public async Task<SystemHealthOverview> GetSystemOverview(CancellationToken token)
    {
        try
        {
            var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");

            // Aggregator grain will cache its overview internally
            var overview = await aggregatorGrain.GetSystemOverview(token);

            return overview;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get system overview");

            // Return fallback empty overview
            return new SystemHealthOverview
            {
                GeneratedAt = DateTime.UtcNow,
                TotalServices = 0,
                HealthyServices = 0,
                ProblematicServices = 0,
                OverallSystemHealth = 0,
                Alerts = new List<SystemHealthAlert>(),
                ServicesByTrend = new Dictionary<string, int>()
            };
        }
    }

    public async Task<HealthTrendComparisonReport> CompareServices(CompareServicesRequest request, CancellationToken token)
    {
        if (!request.ServiceIds.Any())
        {
            logger.LogWarning("No service IDs provided for comparison");
            return new HealthTrendComparisonReport
            {
                AnalysisWindow = TimeSpan.FromHours(request.Hours),
                ServiceComparisons = new List<ServiceTrendComparison>(),
                HealthRanking = new SystemHealthRanking()
            };
        }

        try
        {
            var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");

            // Aggregator grain will coordinate with individual trend grains efficiently
            var comparison = await aggregatorGrain.CompareServiceTrends(
                request.ServiceIds,
                TimeSpan.FromHours(request.Hours), token);

            return comparison;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compare services: {ServiceIds}", string.Join(", ", request.ServiceIds));

            // Return empty comparison report
            return new HealthTrendComparisonReport
            {
                GeneratedAt = DateTime.UtcNow,
                AnalysisWindow = TimeSpan.FromHours(request.Hours),
                ServiceComparisons = new List<ServiceTrendComparison>(),
                HealthRanking = new SystemHealthRanking()
            };
        }
    }

    public async Task RefreshAllTrends(CancellationToken token)
    {
        try
        {
            logger.LogInformation("Starting refresh of all trends");

            var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");

            // This will clear cached data in grains and force fresh calculations
            await aggregatorGrain.RefreshAllTrends(token);

            logger.LogInformation("Successfully refreshed all trends");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh all trends");
            throw;
        }
    }
}