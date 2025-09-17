using HealthMonitor.Model;

namespace HealthMonitor.Services;

public interface IServiceRegistry
{
    ValueTask<IEnumerable<string>> GetAllServiceIds();

    IAsyncEnumerable<Service> GetAllServices(CancellationToken token);

    ValueTask<IEnumerable<ServiceMetadata>> GetAllServicesMetadata();
}