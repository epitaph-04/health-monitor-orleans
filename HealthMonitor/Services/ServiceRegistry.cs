using System.Runtime.CompilerServices;
using HealthMonitor.Grains;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Microsoft.Extensions.Options;

namespace HealthMonitor.Services;

public class ServiceRegistry(IOptions<ServiceConfigurations> options, IClusterClient client) : IServiceRegistry
{
    public ValueTask<IEnumerable<ServiceMetadata>> GetAllServicesMetadata()
    {
        var serviceConfigurations = options.Value;
        return ValueTask.FromResult(serviceConfigurations.Select(serviceConfiguration => new ServiceMetadata
        {
            Id = serviceConfiguration.Id,
            Name = serviceConfiguration.Name,
            ServiceType = serviceConfiguration.Type,
            DependentServices = []
        }));
    }

    public ValueTask<IEnumerable<string>> GetAllServiceIds()
    {
        var serviceConfigurations = options.Value;
        return ValueTask.FromResult( serviceConfigurations.Select(serviceConfiguration => serviceConfiguration.Id));
    }

    public async IAsyncEnumerable<Service> GetAllServices([EnumeratorCancellation] CancellationToken token)
    {
        var serviceConfigurations = options.Value;
        var serviceTasks = serviceConfigurations.Select(async serviceConfiguration =>
        {
            var lastCheck = await client.GetGrain<IHealthCheckGrain>(serviceConfiguration.Id)
                .GetLastRecord(token)
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