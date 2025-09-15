using System.Runtime.CompilerServices;
using HealthMonitor.Grains;
using HealthMonitor.Model;
using Microsoft.Extensions.Options;

namespace HealthMonitor.Services;

public class ApplicationServices(IOptions<ServiceConfigurations> options, IClusterClient client)
{
    public IEnumerable<ServiceMetadata> GetServicesMetadata()
    {
        var serviceConfigurations = options.Value;
        return serviceConfigurations.Select(serviceConfiguration => new ServiceMetadata
        {
            Id = serviceConfiguration.Id,
            Name = serviceConfiguration.Name,
            ServiceType = serviceConfiguration.Type,
            DependentServices = []
        });
    }
    public async IAsyncEnumerable<Service> GetServices([EnumeratorCancellation] CancellationToken token)
    {
        var serviceConfigurations = options.Value;
        var serviceTasks = serviceConfigurations.Select(async serviceConfiguration =>
        {
            var lastCheck = await client.GetGrain<IHealthCheckGrain>(serviceConfiguration.Id)
                .GetLastCheckResult(token)
                .ConfigureAwait(false);
            return new Service
            {
                Id = serviceConfiguration.Id,
                Name = serviceConfiguration.Name,
                ServiceType = serviceConfiguration.Type,
                LastCheckStatus = lastCheck,
                DependentServices = []
            };
        });
        foreach (var task in serviceTasks)
        {
            yield return await task.ConfigureAwait(false);
        }
    }
}