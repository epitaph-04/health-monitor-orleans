using HealthMonitor.Grains;
using HealthMonitor.Model;
using Microsoft.Extensions.Options;

namespace HealthMonitor.Services.BgService;

public class GrainInitializerBackgroundService(IOptions<ServiceConfigurations> options, IClusterClient client)
    : BackgroundService
{
    private readonly ServiceConfigurations _serviceConfigurations = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var configuration in _serviceConfigurations)
        {
            await client.GetGrain<IHealthCheckGrain>(configuration.Id).Register(configuration.IntervalMinutes, CancellationToken.None);
        }
    }
}