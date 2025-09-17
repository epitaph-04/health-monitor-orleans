using HealthMonitor.Model;

namespace HealthMonitor.Services.HealthCheckServices;

public class HealthCheckServiceFactory(IServiceProvider serviceProvider) : IHealthCheckServiceFactory
{
    public IHealthCheckService GetService(string serviceId) => serviceProvider.GetRequiredKeyedService<IHealthCheckService>(serviceId);
}