using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public enum Status
{
    [Id(0)]
    Unknown,
    [Id(1)]
    Healthy,
    [Id(2)]
    Degraded,
    [Id(3)]
    Critical
}