using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public enum Status
{
    Unknown,
    Healthy,
    Degraded,
    Critical
}

public static class StatusExtensions
{
    public static bool IsHealthy(this Status status) =>
        status switch
        {
            Status.Healthy => true,
            _ => false
        };
    public static bool IsCritical(this Status status) =>
        status switch
        {
            Status.Critical => true,
            Status.Degraded => true,
            _ => false
        };
}