using HealthMonitor.Grains;

namespace HealthMonitor.Services.BgService;

public class HealthTrendCalculationService(
    IGrainFactory grainFactory,
    ILogger<HealthTrendCalculationService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue<TimeSpan>("HealthTrends:CalculationInterval", TimeSpan.FromHours(1));
        
        logger.LogInformation("Starting health trend calculation service with interval {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CalculateAllTrends(stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during trend calculation cycle");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
            }
        }
    }

    private async Task CalculateAllTrends(CancellationToken token)
    {
        logger.LogInformation("Starting trend calculation cycle");
        
        var aggregatorGrain = grainFactory.GetGrain<IHealthTrendAggregatorGrain>("system");
        await aggregatorGrain.RefreshAllTrends(token);
        
        logger.LogInformation("Completed trend calculation cycle");
    }
}
