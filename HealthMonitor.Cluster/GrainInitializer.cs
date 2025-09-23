using HealthMonitor.Grains.Abstraction;
using Microsoft.Extensions.Hosting;

namespace HealthMonitor.Cluster;

public class GrainInitializerService(IClusterClient client): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await client.GetGrain<IHealthTrendAggregatorGrain>("system").Initialize(stoppingToken);
    }
}