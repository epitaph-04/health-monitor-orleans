namespace HealthMonitor.Model;

public enum TimeSeriesMetric
{
    ResponseTime,
    ErrorRate,
    Availability,
    Throughput,
    CpuUsage,
    MemoryUsage,
    DiskUsage
}

public enum TrendDirection
{
    Stable,
    Increasing,
    Decreasing,
    Volatile,
    Cyclical
}

public class CorrelationMatrix
{
    public string[] ServiceIds { get; set; } = [];
    public double[,] Matrix { get; set; } = new double[0,0];
    public CorrelationPair[] SignificantCorrelations { get; set; } = [];
}

public class CorrelationPair
{
    public string ServiceA { get; set; } = null!;
    public string ServiceB { get; set; } = null!;
    public double Correlation { get; set; }
    public double PValue { get; set; }
    public CorrelationType Type { get; set; }
    public string Description { get; set; } = null!;
}

public enum CorrelationType
{
    Positive,
    Negative,
    None,
    NonLinear
}

public class ServiceImpact
{
    public string ServiceId { get; set; } = null!;
    public double ImpactScore { get; set; }
    public string[] AffectedServices { get; set; } = [];
    public ImpactType Type { get; set; }
    public string Description { get; set; } = null!;
}

public enum ImpactType
{
    UpstreamDependency,
    DownstreamEffect,
    SharedResource,
    NetworkEffect
}

public class CascadeFailureRisk
{
    public string OriginService { get; set; } = null!;
    public string[] AffectedServices { get; set; } = [];
    public double RiskScore { get; set; }
    public CascadePath[] FailurePaths { get; set; } = [];
    public string[] MitigationStrategies { get; set; } = [];
}

public class CascadePath
{
    public string[] ServiceChain { get; set; } = [];
    public double Probability { get; set; }
    public TimeSpan EstimatedPropagationTime { get; set; }
    public string Description { get; set; } = null!;
}

public class GrowthProjection
{
    public TimeSeriesMetric Metric { get; set; }
    public DateTime ProjectionDate { get; set; }
    public double ProjectedValue { get; set; }
    public double GrowthRate { get; set; }
    public double Confidence { get; set; }
    public GrowthPattern Pattern { get; set; }
}

public enum GrowthPattern
{
    Linear,
    Exponential,
    Logarithmic,
    Seasonal,
    Volatile
}

public class ResourceRequirement
{
    public ResourceType Type { get; set; }
    public double CurrentUsage { get; set; }
    public double ProjectedUsage { get; set; }
    public double RecommendedCapacity { get; set; }
    public DateTime WhenNeeded { get; set; }
    public string Justification { get; set; } = null!;
}

public enum ResourceType
{
    Cpu,
    Memory,
    Storage,
    Network,
    Instances,
    Database
}

public class ScalingRecommendation
{
    public ResourceType Resource { get; set; }
    public ScalingAction Action { get; set; }
    public double Scale { get; set; }
    public DateTime RecommendedTime { get; set; }
    public string Reasoning { get; set; } = null!;
    public double CostImpact { get; set; }
    public Priority Priority { get; set; }
}

public enum ScalingAction
{
    ScaleUp,
    ScaleDown,
    Maintain,
    Optimize
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public class RegressionDetection
{
    public TimeSeriesMetric Metric { get; set; }
    public DateTime DetectedAt { get; set; }
    public double BaselineValue { get; set; }
    public double CurrentValue { get; set; }
    public double PercentageChange { get; set; }
    public RegressionSeverity Severity { get; set; }
    public string Description { get; set; } = null!;
}

public enum RegressionSeverity
{
    Minor,
    Moderate,
    Significant,
    Critical
}

public class PerformanceBaseline
{
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public double Throughput { get; set; }
    public double Availability { get; set; }
    public DateTime BaselineDate { get; set; }
    public TimeSpan BaselinePeriod { get; set; }
}

public class ServiceBenchmark
{
    public string ServiceId { get; set; } = null!;
    public BenchmarkMetric[] Metrics { get; set; } = [];
    public ServiceComparison[] Comparisons { get; set; } = [];
    public BenchmarkRanking Ranking { get; set; } = new();
    public string[] Insights { get; set; } = [];
}

public class BenchmarkMetric
{
    public TimeSeriesMetric Type { get; set; }
    public double Value { get; set; }
    public double PercentileRank { get; set; }
    public BenchmarkGrade Grade { get; set; }
    public string Description { get; set; } = null!;
}

public enum BenchmarkGrade
{
    Excellent,
    Good,
    Average,
    BelowAverage,
    Poor
}

public class ServiceComparison
{
    public string ComparedServiceId { get; set; } = null!;
    public ComparisonResult[] Results { get; set; } = [];
    public double OverallScore { get; set; }
    public string Summary { get; set; } = null!;
}

public class ComparisonResult
{
    public TimeSeriesMetric Metric { get; set; }
    public double ServiceValue { get; set; }
    public double ComparedValue { get; set; }
    public double Difference { get; set; }
    public bool IsBetter { get; set; }
}

public class BenchmarkRanking
{
    public int OverallRank { get; set; }
    public int TotalServices { get; set; }
    public Dictionary<TimeSeriesMetric, int> MetricRanks { get; set; } = new();
    public double PerformanceScore { get; set; }
}

public class AlertEffectivenessReport
{
    public TimeSpan Period { get; set; }
    public AlertMetrics Overall { get; set; } = new();
    public AlertChannelEffectiveness[] ChannelEffectiveness { get; set; } = [];
    public AlertTypeAnalysis[] TypeAnalysis { get; set; } = [];
    public FalsePositiveAnalysis FalsePositives { get; set; } = new();
    public string[] Recommendations { get; set; } = [];
}

public class AlertMetrics
{
    public int TotalAlerts { get; set; }
    public int TruePositives { get; set; }
    public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
}

public class AlertChannelEffectiveness
{
    public string ChannelId { get; set; } = null!;
    public string ChannelType { get; set; } = null!;
    public double DeliveryRate { get; set; }
    public TimeSpan AverageDeliveryTime { get; set; }
    public double EngagementRate { get; set; }
    public string[] Issues { get; set; } = [];
}

public class AlertTypeAnalysis
{
    public AlertLevel Level { get; set; }
    public int Count { get; set; }
    public double AccuracyRate { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
    public string[] CommonCauses { get; set; } = [];
}

public class FalsePositiveAnalysis
{
    public int Count { get; set; }
    public double Rate { get; set; }
    public FalsePositivePattern[] Patterns { get; set; } = [];
    public string[] RecommendedAdjustments { get; set; } = [];
}

public class FalsePositivePattern
{
    public string Pattern { get; set; } = null!;
    public int Frequency { get; set; }
    public string[] AffectedServices { get; set; } = [];
    public string SuggestedFix { get; set; } = null!;
}

public class SystemInsights
{
    public TimeSpan Period { get; set; }
    public SystemHealthOverview HealthOverview { get; set; } = new();
    public PerformanceTrend[] Trends { get; set; } = [];
    public CriticalInsight[] CriticalInsights { get; set; } = [];
    public OptimizationOpportunity[] Opportunities { get; set; } = [];
    public RiskAssessment[] Risks { get; set; } = [];
    public BusinessImpact BusinessImpact { get; set; } = new();
}

public class SystemHealthOverview
{
    public double OverallHealthScore { get; set; }
    public int TotalServices { get; set; }
    public ServiceStatusDistribution StatusDistribution { get; set; } = new();
    public TopIssue[] TopIssues { get; set; } = [];
    public HealthTrendSummary TrendSummary { get; set; } = new();
}

public class ServiceStatusDistribution
{
    public int Healthy { get; set; }
    public int Degraded { get; set; }
    public int Critical { get; set; }
    public int Unknown { get; set; }
}

public class TopIssue
{
    public string Description { get; set; } = null!;
    public string[] AffectedServices { get; set; } = [];
    public Priority Severity { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public int Frequency { get; set; }
}

public class HealthTrendSummary
{
    public TrendDirection OverallTrend { get; set; }
    public double TrendStrength { get; set; }
    public string[] ImprovingServices { get; set; } = [];
    public string[] DegradingServices { get; set; } = [];
}

public class PerformanceTrend
{
    public TimeSeriesMetric Metric { get; set; }
    public TrendDirection Direction { get; set; }
    public double ChangePercentage { get; set; }
    public Priority Significance { get; set; }
    public string Description { get; set; } = null!;
}

public class CriticalInsight
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Priority Priority { get; set; }
    public string[] AffectedServices { get; set; } = [];
    public string[] RecommendedActions { get; set; } = [];
    public double ImpactScore { get; set; }
}

public class OptimizationOpportunity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public OptimizationType Type { get; set; }
    public double PotentialImprovement { get; set; }
    public string[] RequiredActions { get; set; } = [];
    public double EstimatedEffort { get; set; }
    public double Roi { get; set; }
}

public enum OptimizationType
{
    Performance,
    Cost,
    Reliability,
    Security,
    Scalability
}

public class RiskAssessment
{
    public string RiskDescription { get; set; } = null!;
    public double Probability { get; set; }
    public double Impact { get; set; }
    public double RiskScore { get; set; }
    public RiskCategory Category { get; set; }
    public string[] MitigationStrategies { get; set; } = [];
    public DateTime IdentifiedAt { get; set; }
}

public enum RiskCategory
{
    Technical,
    Operational,
    Security,
    Capacity,
    Dependency
}

public class BusinessImpact
{
    public double EstimatedDowntimeCost { get; set; }
    public double PerformanceImpactCost { get; set; }
    public CustomerImpact CustomerImpact { get; set; } = new();
    public SlaImpact SlaImpact { get; set; } = new();
    public string[] BusinessRecommendations { get; set; } = [];
}

public class CustomerImpact
{
    public int AffectedUsers { get; set; }
    public double UserExperienceScore { get; set; }
    public string[] ImpactedFeatures { get; set; } = [];
    public TimeSpan TotalUserWaitTime { get; set; }
}

public class SlaImpact
{
    public string[] ViolatedSlas { get; set; } = [];
    public double ComplianceScore { get; set; }
    public double PenaltyCost { get; set; }
    public string[] RiskySlas { get; set; } = [];
}

public class CustomAnalyticsQuery
{
    public string QueryName { get; set; } = null!;
    public string[] ServiceIds { get; set; } = [];
    public TimeSpan Period { get; set; }
    public string[] Metrics { get; set; } = [];
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string QueryType { get; set; } = null!;
}

public class CustomAnalyticsReport
{
    public string QueryName { get; set; } = null!;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public object Data { get; set; } = null!;
    public string[] Insights { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class DataExportRequest
{
    public string[] ServiceIds { get; set; } = [];
    public TimeSpan Period { get; set; }
    public ExportFormat Format { get; set; }
    public string[] Metrics { get; set; } = [];
    public bool IncludeMetadata { get; set; } = true;
    public string? FilterExpression { get; set; }
}

public enum ExportFormat
{
    JSON,
    CSV,
    Excel,
    XML,
    Parquet
}

public class DataExport
{
    public string ExportId { get; set; } = Guid.NewGuid().ToString();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public ExportFormat Format { get; set; }
    public byte[] Data { get; set; } = [];
    public long SizeBytes { get; set; }
    public int RecordCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SeasonalPattern
{
    public TimeSpan Period { get; set; }
    public double Amplitude { get; set; }
    public TimeSpan Phase { get; set; }
    public double Confidence { get; set; }
    public string Description { get; set; } = null!;
}

public class HealthTrendData
{
    public TimeSpan Period { get; set; }
    public HealthTrendPoint[] DataPoints { get; set; } = [];
    public double CurrentHealthScore { get; set; }
    public double AverageHealthScore { get; set; }
    public TrendDirection Trend { get; set; }
    public double TrendStrength { get; set; }
    public string[] ImprovingServices { get; set; } = [];
    public string[] DegradingServices { get; set; } = [];
}

public class HealthTrendPoint
{
    public DateTime Timestamp { get; set; }
    public double HealthScore { get; set; }
    public int HealthyServices { get; set; }
    public int DegradedServices { get; set; }
    public int CriticalServices { get; set; }
    public int TotalServices { get; set; }
}