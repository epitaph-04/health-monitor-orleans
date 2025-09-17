using Orleans;

namespace HealthMonitor.Model.Analytics;

[GenerateSerializer]
public enum TrendDirection
{
    [Id(0)]
    Stable,
    [Id(1)]
    Increasing,
    [Id(2)]
    Decreasing
}

[GenerateSerializer]
public record HealthDataPoint(DateTime Timestamp, double HealthScore);

[GenerateSerializer]
public record ServiceTrendResult(string ServiceId, TrendDirection Direction, double Strength);

[GenerateSerializer]
public record SystemHealthTrend
{
    [Id(0)]
    public TimeSpan Period { get; init; }
    [Id(1)]
    public double CurrentOverallHealthScore { get; init; }
    [Id(2)]
    public double AverageOverallHealthScore { get; init; }
    [Id(3)]
    public TrendDirection OverallTrend { get; init; }
    [Id(4)]
    public double OverallTrendStrength { get; init; }
    [Id(5)]
    public List<HealthDataPoint> AggregatedDataPoints { get; init; } = [];
    [Id(6)]
    public List<ServiceTrendResult> TopImprovingServices { get; init; } = [];
    [Id(7)]
    public List<ServiceTrendResult> TopDegradingServices { get; init; } = [];
}