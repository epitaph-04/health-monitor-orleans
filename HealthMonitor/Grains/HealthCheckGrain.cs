using HealthMonitor.Model;
using HealthMonitor.Model.Analytics;
using HealthMonitor.Services.HealthCheckServices;
using Orleans.Providers;

namespace HealthMonitor.Grains;

public interface IHealthCheckGrain : IGrainWithStringKey
{
    public ValueTask Register(int minutes, CancellationToken token);
    ValueTask<List<HealthCheckRecord>> GetHealthRecords(DateTime fromTime, DateTime toTime, CancellationToken token);
    ValueTask<List<HealthCheckRecord>> GetRecentRecords(TimeSpan timespan, CancellationToken token);
    ValueTask<HealthCheckRecord> GetLastRecord(CancellationToken token);
    ValueTask<HealthDataStatistics> GetStatistics(DateTime fromTime, DateTime toTime, CancellationToken token);
    ValueTask<int> GetRecordCount(CancellationToken token);
    ValueTask CleanupOldRecords(DateTime cutoffTime, CancellationToken token);
}

[GenerateSerializer]
public class HealthDataState
{
    [Id(0)]
    public string ServiceId { get; set; } = "";
    [Id(1)]
    public List<HealthCheckRecord> Records { get; set; } = new();
    [Id(2)]
    public DateTime LastUpdated { get; set; }
}

[StorageProvider(ProviderName = "Default")]
public class HealthCheckGrain(
    IClusterClient client,
    IHealthCheckServiceFactory factory,
    ILogger<HealthCheckGrain> logger
    ) : Grain<HealthDataState>, IHealthCheckGrain, IRemindable
{
    private const int MaxRecordsPerGrain = 129600;
    private const string HealthCheckReminder = "HEALTH_CHECK_REMINDER";
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (State.ServiceId.IsWhiteSpace())
        {
            State.ServiceId = this.GetPrimaryKeyString();
            State.Records = new List<HealthCheckRecord>();
        }
        await base.OnActivateAsync(cancellationToken);
    }

    public async ValueTask Register(int minutes, CancellationToken token)
    {
        await this.RegisterOrUpdateReminder(
            $"{HealthCheckReminder}-{this.GetPrimaryKeyString()}", 
            TimeSpan.FromMinutes(minutes),
            TimeSpan.FromMinutes(minutes));
    }

    public ValueTask<HealthCheckRecord> GetLastRecord(CancellationToken token) 
        => ValueTask.FromResult(State.Records.Any() ? State.Records.Last() : new());
    
    public ValueTask<List<HealthCheckRecord>> GetHealthRecords(DateTime fromTime, DateTime toTime, CancellationToken token)
    {
        var filteredRecords = State.Records
            .Where(r => r.Timestamp >= fromTime && r.Timestamp <= toTime)
            .OrderBy(r => r.Timestamp)
            .ToList();
        
        return ValueTask.FromResult(filteredRecords);
    }

    public ValueTask<List<HealthCheckRecord>> GetRecentRecords(TimeSpan timespan, CancellationToken token)
    {
        var cutoff = DateTime.UtcNow - timespan;
        return GetHealthRecords(cutoff, DateTime.UtcNow, token);
    }

    public ValueTask<HealthDataStatistics> GetStatistics(DateTime fromTime, DateTime toTime, CancellationToken token)
    {
        var records = State.Records.Where(r => r.Timestamp >= fromTime && r.Timestamp <= toTime).ToList();
        
        if (!records.Any())
        {
            return ValueTask.FromResult(new HealthDataStatistics());
        }

        var healthyRecords = records.Where(r => r.Status.IsHealthy()).ToList();
        var responseTimes = records.Where(r => r.ResponseTime > TimeSpan.Zero).Select(r => r.ResponseTime).ToList();

        var stats = new HealthDataStatistics
        {
            TotalRecords = records.Count,
            AvailabilityPercentage = (double)healthyRecords.Count / records.Count * 100,
            FailureCount = records.Count - healthyRecords.Count,
            FirstRecord = records.Min(r => r.Timestamp),
            LastRecord = records.Max(r => r.Timestamp)
        };

        if (responseTimes.Any())
        {
            stats.AverageResponseTime = TimeSpan.FromTicks((long)responseTimes.Average(rt => rt.Ticks));
            stats.MaxResponseTime = responseTimes.Max();
            stats.MinResponseTime = responseTimes.Min();
        }

        return ValueTask.FromResult(stats);
    }

    public ValueTask<int> GetRecordCount(CancellationToken token)
    {
        return ValueTask.FromResult(State.Records.Count);
    }

    public async ValueTask CleanupOldRecords(DateTime cutoffTime, CancellationToken token)
    {
        var originalCount = State.Records.Count;
        State.Records = State.Records.Where(r => r.Timestamp > cutoffTime).ToList();
        
        if (State.Records.Count != originalCount)
        {
            await WriteStateAsync();
            logger.LogInformation("Cleaned up {Count} old records for service {ServiceId}", 
                originalCount - State.Records.Count, State.ServiceId);
        }
    }
    
    async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        CancellationToken token = new CancellationToken(false);
        var healthCheckRecord = await factory.GetService(this.GetPrimaryKeyString()).CheckHealthAsync();
        
        State.Records.Add(healthCheckRecord);
        State.LastUpdated = DateTime.UtcNow;
        
        if (State.Records.Count > MaxRecordsPerGrain)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            State.Records = State.Records.Where(r => r.Timestamp > cutoff).ToList();
        }
        await WriteStateAsync();
        await client.GetGrain<INotifierGrains>(0).Notify(this.GetPrimaryKeyString(), healthCheckRecord);
    }
}