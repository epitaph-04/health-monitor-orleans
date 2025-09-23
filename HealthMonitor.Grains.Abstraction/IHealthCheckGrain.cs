using HealthMonitor.Model;
using Orleans;

namespace HealthMonitor.Grains.Abstraction;

public interface IHealthCheckGrain : IGrainWithStringKey
{
    ValueTask Register(ServiceConfiguration configuration, CancellationToken token);
    ValueTask<List<HealthCheckRecord>> GetHealthRecords(DateTime fromTime, DateTime toTime, CancellationToken token);
    ValueTask<List<HealthCheckRecord>> GetRecentRecords(TimeSpan timespan, CancellationToken token);
    ValueTask<HealthCheckRecord> GetLastRecord(CancellationToken token);
    ValueTask<HealthDataStatistics> GetStatistics(DateTime fromTime, DateTime toTime, CancellationToken token);
    ValueTask<int> GetRecordCount(CancellationToken token);
    ValueTask CleanupOldRecords(DateTime cutoffTime, CancellationToken token);
}