using HealthMonitor.Model;
using HealthMonitor.Services.HealthCheckServices;
using Orleans.Providers;

namespace HealthMonitor.Grains;

public interface IHealthCheckGrain : IGrainWithStringKey
{
    public ValueTask Register(int minutes, CancellationToken token);
    public ValueTask<HealthCheckResult> GetLastCheckResult(CancellationToken token);
    public ValueTask<List<HealthCheckResult>> GetHistoricalCheckResults(CancellationToken token);
}

[StorageProvider(ProviderName = "Default")]
public class HealthCheckGrain(
    IClusterClient client,
    [PersistentState("healthCheckState")] IPersistentState<HealthCheckResult> healthCheckState,
    [PersistentState("historicalHealthChecksState")] IPersistentState<List<HealthCheckResult>> historicalHealthChecksState,
    IHealthCheckServiceFactory factory
    ) : Grain, IHealthCheckGrain, IRemindable
{
    private const string HealthCheckReminder = "HEALTH_CHECK_REMINDER";
    public async ValueTask Register(int minutes, CancellationToken token)
    {
        historicalHealthChecksState.State = [];
        // await ValueTask.CompletedTask;
        await this.RegisterOrUpdateReminder(
            $"{HealthCheckReminder}-{this.GetPrimaryKeyString()}", 
            TimeSpan.FromMinutes(minutes),
            TimeSpan.FromMinutes(minutes));
        await historicalHealthChecksState.WriteStateAsync(token);
    }

    public ValueTask<HealthCheckResult> GetLastCheckResult(CancellationToken token) 
        => ValueTask.FromResult(healthCheckState.State);

    public ValueTask<List<HealthCheckResult>> GetHistoricalCheckResults(CancellationToken token)
        => ValueTask.FromResult(historicalHealthChecksState.State);

    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        CancellationToken token = new CancellationToken(false);
        var healthCheckResult = await factory.GetService(this.GetPrimaryKeyString()).CheckHealthAsync();
        healthCheckState.State = healthCheckResult;
        historicalHealthChecksState.State.Add(healthCheckResult);
        await healthCheckState.WriteStateAsync(token);
        await historicalHealthChecksState.WriteStateAsync(token);
        await client.GetGrain<INotifierGrains>(0).Notify(this.GetPrimaryKeyString(), healthCheckResult);
    }
}