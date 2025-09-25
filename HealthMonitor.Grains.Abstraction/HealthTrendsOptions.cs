using Orleans;

namespace HealthMonitor.Grains.Abstraction;

[GenerateSerializer]
[Alias("HealthMonitor.Grains.Abstraction.HealthTrendsOptions")]
public class HealthTrendsOptions
{
    [Id(0)]
    public TimeSpan CalculationInterval { get; set; } = TimeSpan.FromMinutes(15);
    [Id(1)]
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
    [Id(2)]
    public SlaTarget SlaTargets { get; set; } = new();
}

[GenerateSerializer]
[Alias("HealthMonitor.Grains.Abstraction.SlaTarget")]
public class SlaTarget
{
    [Id(0)] public double AvailabilityPercentage { get; set; } = 99.9;
    [Id(1)] public double ResponseTimeSeconds { get; set; } = 1.0;
}