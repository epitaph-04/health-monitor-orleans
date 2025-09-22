using HealthMonitor.Model.Analytics;

namespace HealthMonitor.Model;

public record Service : ServiceMetadata
{
    public HealthCheckRecord LastCheckStatus { get; init; } = null!;
    public Queue<HealthCheckRecord> HistoricStatus { get; init; } = new(5);
}