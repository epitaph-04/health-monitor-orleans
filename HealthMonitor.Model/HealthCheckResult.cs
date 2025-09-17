using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public record HealthCheckResult
{
    [Id(0)]
    public Status Status { get; set; } = Status.Unknown;
    [Id(1)]
    public TimeSpan ResponseTime { get; set; }
    [Id(2)]
    public DateTime CheckedTimeUtc { get; set; } = DateTime.UtcNow;
    [Id(3)]
    public string Message { get; set; } = null!;
}