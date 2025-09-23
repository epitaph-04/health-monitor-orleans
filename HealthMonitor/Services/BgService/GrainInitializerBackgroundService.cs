using HealthMonitor.Grains;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Microsoft.Extensions.Options;

namespace HealthMonitor.Services.BgService;

public class GrainInitializerBackgroundService(IOptions<ServiceConfigurations> options, IClusterClient client)
    : BackgroundService
{
    private readonly ServiceConfigurations _serviceConfigurations = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var aggregatorGrain = client.GetGrain<IHealthTrendAggregatorGrain>("system");
        foreach (var configuration in _serviceConfigurations)
        {
            await aggregatorGrain.RegisterService(configuration, stoppingToken);
        }
    }
}