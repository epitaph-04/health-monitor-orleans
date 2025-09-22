using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public record HealthCheckRecord
{
    [Id(0)]
    public DateTime Timestamp { get; set; }
    [Id(1)]
    public Status Status { get; set; }
    [Id(2)]
    public TimeSpan ResponseTime { get; set; }
    [Id(3)]
    public string? ErrorMessage { get; set; }
    [Id(4)]
    public Dictionary<string, object> Metadata { get; set; } = new();
}