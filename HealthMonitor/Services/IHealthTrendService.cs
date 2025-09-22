using HealthMonitor.Model;
using HealthMonitor.Model.Analytics;

namespace HealthMonitor.Services;

public interface IHealthTrendService
{
    public Task<HealthTrendData> GetServiceTrend(string serviceId, int hours, CancellationToken token);

    public Task<List<HealthTrendData>> GetServiceTrendHistory(string serviceId, int count, CancellationToken token);

    public Task<SystemHealthOverview> GetSystemOverview(CancellationToken token);

    public Task<HealthTrendComparisonReport> CompareServices(CompareServicesRequest request, CancellationToken token);

    public Task RefreshAllTrends(CancellationToken token);
}