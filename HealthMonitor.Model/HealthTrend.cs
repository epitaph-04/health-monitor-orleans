using Orleans;

namespace HealthMonitor.Model;

[GenerateSerializer]
public class HealthTrendData
{
    [Id(0)]
    public string ServiceId { get; set; } = "";
    [Id(1)]
    public DateTime CalculatedAt { get; set; }
    [Id(2)]
    public TimeSpan TimeWindow { get; set; }

    // Overall metrics
    [Id(3)]
    public double OverallHealthScore { get; set; }
    [Id(4)]
    public double AvailabilityPercentage { get; set; }
    [Id(5)]
    public TimeSpan AverageResponseTime { get; set; }
    [Id(6)]
    public int TotalDataPoints { get; set; }

    // Trend indicators
    [Id(7)]
    public HealthTrendDirection HealthTrend { get; set; }
    [Id(8)]
    public HealthTrendDirection ResponseTimeTrend { get; set; }
    [Id(9)]
    public double TrendConfidence { get; set; } // 0-1, how confident we are in the trend

    // Time-based breakdowns
    [Id(10)]
    public List<HourlyHealthSummary> HourlyBreakdown { get; set; } = new();
    [Id(11)]
    public List<DailyHealthSummary> DailyBreakdown { get; set; } = new();
    [Id(12)]
    public List<WeeklyHealthSummary> WeeklyBreakdown { get; set; } = new();

    // Anomaly detection
    [Id(13)]
    public List<HealthAnomaly> DetectedAnomalies { get; set; } = new();

    // Service Level Objectives
    [Id(14)]
    public SlaMetrics SlaMetrics { get; set; } = new();

    // Predictions
    [Id(15)]
    public List<HealthPrediction> Predictions { get; set; } = new();
}

[GenerateSerializer]
public enum HealthTrendDirection
{
    Improving,
    Stable,
    Declining,
    Volatile,
    Unknown
}

[GenerateSerializer]
public record HourlyHealthSummary
{
    [Id(0)]
    public DateTime Hour { get; set; }
    [Id(1)]
    public double HealthScore { get; set; }
    [Id(2)]
    public double AvailabilityPercentage { get; set; }
    [Id(3)]
    public TimeSpan AverageResponseTime { get; set; }
    [Id(4)]
    public int DataPoints { get; set; }
    [Id(5)]
    public int FailureCount { get; set; }
}

[GenerateSerializer]
public record DailyHealthSummary
{
    [Id(0)]
    public DateTime Date { get; set; }
    [Id(1)]
    public double HealthScore { get; set; }
    [Id(2)]
    public double AvailabilityPercentage { get; set; }
    [Id(3)]
    public TimeSpan AverageResponseTime { get; set; }
    [Id(4)]
    public TimeSpan MaxResponseTime { get; set; }
    [Id(5)]
    public int TotalChecks { get; set; }
    [Id(6)]
    public int FailedChecks { get; set; }
    [Id(7)]
    public List<string> CommonErrors { get; set; } = new();
}

[GenerateSerializer]
public record WeeklyHealthSummary
{
    [Id(0)]
    public DateTime WeekStarting { get; set; }
    [Id(1)]
    public double HealthScore { get; set; }
    [Id(2)]
    public double AvailabilityPercentage { get; set; }
    [Id(3)]
    public TimeSpan AverageResponseTime { get; set; }
    [Id(4)]
    public int TotalChecks { get; set; }
    [Id(5)]
    public int FailedChecks { get; set; }
    [Id(6)]
    public HealthTrendDirection WeekTrend { get; set; }
}

[GenerateSerializer]
public class HealthAnomaly
{
    [Id(0)]
    public DateTime StartTime { get; set; }
    [Id(1)]
    public DateTime EndTime { get; set; }
    [Id(2)]
    public AnomalyType Type { get; set; }
    [Id(3)]
    public double Severity { get; set; } // 0-1
    [Id(4)]
    public string Description { get; set; } = "";
    [Id(5)]
    public Dictionary<string, object> Details { get; set; } = new();
}

[GenerateSerializer]
public enum AnomalyType
{
    HighResponseTime,
    FrequentFailures,
    LongOutage,
    UnusualPattern,
    PerformanceDegradation
}

[GenerateSerializer]
public record SlaMetrics
{
    [Id(0)]
    public double TargetAvailability { get; set; } = 0.999; // 99.9%
    [Id(1)]
    public TimeSpan TargetResponseTime { get; set; } = TimeSpan.FromSeconds(1);
    [Id(2)]
    public double ActualAvailability { get; set; }
    [Id(3)]
    public TimeSpan ActualAverageResponseTime { get; set; }
    [Id(4)]
    public bool MeetingAvailabilitySla { get; set; }
    [Id(5)]
    public bool MeetingResponseTimeSla { get; set; }
    [Id(6)]
    public double ErrorBudgetRemaining { get; set; }
    [Id(7)]
    public TimeSpan EstimatedDowntimeThisMonth { get; set; }
}

[GenerateSerializer]
public record HealthPrediction
{
    [Id(0)]
    public DateTime PredictionTime { get; set; }
    [Id(1)]
    public double PredictedHealthScore { get; set; }
    [Id(2)]
    public double Confidence { get; set; }
    [Id(3)]
    public string ReasoningFactors { get; set; } = "";
}

[GenerateSerializer]
public record HealthDataStatistics
{
    [Id(0)]
    public int TotalRecords { get; set; }
    [Id(1)]
    public double AvailabilityPercentage { get; set; }
    [Id(2)]
    public TimeSpan AverageResponseTime { get; set; }
    [Id(3)]
    public TimeSpan MaxResponseTime { get; set; }
    [Id(4)]
    public TimeSpan MinResponseTime { get; set; }
    [Id(5)]
    public int FailureCount { get; set; }
    [Id(6)]
    public DateTime FirstRecord { get; set; }
    [Id(7)]
    public DateTime LastRecord { get; set; }
}

[GenerateSerializer]
public class HealthTrendComparisonReport
{
    [Id(0)]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    [Id(1)]
    public TimeSpan AnalysisWindow { get; set; }
    [Id(2)]
    public List<ServiceTrendComparison> ServiceComparisons { get; set; } = new();
    [Id(3)]
    public SystemHealthRanking HealthRanking { get; set; } = new();
}

[GenerateSerializer]
public class ServiceTrendComparison
{
    [Id(0)]
    public string ServiceId { get; set; } = "";
    [Id(1)]
    public HealthTrendData TrendData { get; set; } = new();
    [Id(2)]
    public int HealthRank { get; set; }
    [Id(3)]
    public double RelativeHealthScore { get; set; }
    [Id(4)]
    public List<string> HealthInsights { get; set; } = new();
}

[GenerateSerializer]
public class SystemHealthRanking
{
    [Id(0)]
    public List<string> HealthiestServices { get; set; } = new();
    [Id(1)]
    public List<string> ProblematicServices { get; set; } = new();
    [Id(2)]
    public List<string> ImprovingServices { get; set; } = new();
    [Id(3)]
    public List<string> DecliningServices { get; set; } = new();
}

[GenerateSerializer]
public class SystemHealthOverview
{
    [Id(0)]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    [Id(1)]
    public int TotalServices { get; set; }
    [Id(2)]
    public int HealthyServices { get; set; }
    [Id(3)]
    public int ProblematicServices { get; set; }
    [Id(4)]
    public double OverallSystemHealth { get; set; }
    [Id(5)]
    public List<SystemHealthAlert> Alerts { get; set; } = new();
    [Id(6)]
    public Dictionary<string, int> ServicesByTrend { get; set; } = new();
}

[GenerateSerializer]
public record SystemHealthAlert
{
    [Id(0)]
    public string ServiceId { get; set; } = "";
    [Id(1)]
    public AlertSeverity Severity { get; set; }
    [Id(2)]
    public string Message { get; set; } = "";
    [Id(3)]
    public DateTime DetectedAt { get; set; }
}

[GenerateSerializer]
public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}