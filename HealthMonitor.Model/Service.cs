namespace HealthMonitor.Model;

public record Service : ServiceMetadata
{
    public HealthCheckResult LastCheckStatus { get; init; } = null!;
    public Queue<HealthCheckResult> HistoricStatus { get; init; } = new(5);
}