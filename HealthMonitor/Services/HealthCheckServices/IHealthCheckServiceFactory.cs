using HealthMonitor.Model;

namespace HealthMonitor.Services.HealthCheckServices;

public interface IHealthCheckServiceFactory
{
    public IHealthCheckService GetService(string serviceId);
}