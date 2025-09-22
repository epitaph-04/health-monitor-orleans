using HealthMonitor.Model;
using HealthMonitor.Model.Analytics;

namespace HealthMonitor.Services.HealthCheckServices;

public interface IHealthCheckService
{
    ServiceConfiguration ServiceConfiguration { get; }
    ValueTask<HealthCheckRecord> CheckHealthAsync();
}