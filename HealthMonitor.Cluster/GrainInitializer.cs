using HealthMonitor.Grains.Abstraction;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;

namespace HealthMonitor.Cluster;

public class GrainInitializerService(IOptions<HealthTrendsOptions> options, IClusterClient client): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await client.GetGrain<IHealthTrendAggregatorGrain>("system").Initialize(options.Value, stoppingToken);
    }
}