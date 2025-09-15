using HealthMonitor.Model;

namespace HealthMonitor.Services.HealthCheckServices;

public interface IHealthCheckService
{
    ServiceConfiguration ServiceConfiguration { get; }
    ValueTask<HealthCheckResult> CheckHealthAsync();
}