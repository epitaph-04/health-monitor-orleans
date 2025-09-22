using HealthMonitor.Model;
using HealthMonitor.Model.Analytics;

namespace HealthMonitor.Services;

public class HealthTrendCalculator
{
    private readonly ILogger<HealthTrendCalculator> _logger;

    public HealthTrendCalculator(ILogger<HealthTrendCalculator> logger)
    {
        _logger = logger;
    }

    public ValueTask<HealthTrendData> CalculateTrend(
        string serviceId, 
        List<HealthCheckRecord> records, 
        TimeSpan analysisWindow)
    {
        if (!records.Any())
        {
            return ValueTask.FromResult(new HealthTrendData 
            { 
                ServiceId = serviceId, 
                TimeWindow = analysisWindow,
                CalculatedAt = DateTime.UtcNow
            });
        }

        var trendData = new HealthTrendData
        {
            ServiceId = serviceId,
            CalculatedAt = DateTime.UtcNow,
            TimeWindow = analysisWindow,
            TotalDataPoints = records.Count
        };

        // Calculate basic metrics
        CalculateBasicMetrics(records, trendData);
        
        // Calculate time-based breakdowns
        trendData.HourlyBreakdown = CalculateHourlyBreakdown(records);
        trendData.DailyBreakdown = CalculateDailyBreakdown(records);
        trendData.WeeklyBreakdown = CalculateWeeklyBreakdown(records);
        
        // Calculate trends
        trendData.HealthTrend = CalculateHealthTrend(records);
        trendData.ResponseTimeTrend = CalculateResponseTimeTrend(records);
        trendData.TrendConfidence = CalculateTrendConfidence(records);
        
        // Detect anomalies
        trendData.DetectedAnomalies = DetectAnomalies(records);
        
        // Calculate SLA metrics
        trendData.SlaMetrics = CalculateSlaMetrics(records);
        
        // Generate predictions
        trendData.Predictions = GeneratePredictions(records);

        return ValueTask.FromResult(trendData);
    }

    private void CalculateBasicMetrics(List<HealthCheckRecord> records, HealthTrendData trendData)
    {
        var healthyRecords = records.Where(r => r.Status.IsHealthy()).ToList();
        var responseTimes = records.Where(r => r.ResponseTime > TimeSpan.Zero).Select(r => r.ResponseTime).ToList();

        trendData.AvailabilityPercentage = (double)healthyRecords.Count / records.Count * 100;
        trendData.OverallHealthScore = CalculateHealthScore(records);
        
        if (responseTimes.Any())
        {
            trendData.AverageResponseTime = TimeSpan.FromTicks((long)responseTimes.Average(rt => rt.Ticks));
        }
    }

    private double CalculateHealthScore(List<HealthCheckRecord> records)
    {
        if (!records.Any()) return 0;

        // Weight recent records more heavily
        var now = DateTime.UtcNow;
        var weightedScore = 0.0;
        var totalWeight = 0.0;

        foreach (var record in records)
        {
            var age = now - record.Timestamp;
            var weight = Math.Exp(-age.TotalHours / 24.0); // Exponential decay over 24 hours
            
            var recordScore = record.Status.IsHealthy() ? 100.0 : 0.0;
            
            // Adjust score based on response time
            if (record.Status.IsHealthy() && record.ResponseTime > TimeSpan.Zero)
            {
                var responseTimePenalty = Math.Min(record.ResponseTime.TotalSeconds / 10.0, 50.0); // Max 50% penalty for 10s+ response
                recordScore -= responseTimePenalty;
            }

            weightedScore += recordScore * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? weightedScore / totalWeight : 0;
    }

    private List<HourlyHealthSummary> CalculateHourlyBreakdown(List<HealthCheckRecord> records)
    {
        return records
            .GroupBy(r => new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, r.Timestamp.Hour, 0, 0))
            .Select(g => new HourlyHealthSummary
            {
                Hour = g.Key,
                DataPoints = g.Count(),
                FailureCount = g.Count(r => r.Status.IsCritical()),
                AvailabilityPercentage = (double)g.Count(r => r.Status.IsHealthy()) / g.Count() * 100,
                HealthScore = CalculateHealthScore(g.ToList()),
                AverageResponseTime = g.Any(r => r.ResponseTime > TimeSpan.Zero) 
                    ? TimeSpan.FromTicks((long)g.Where(r => r.ResponseTime > TimeSpan.Zero).Average(r => r.ResponseTime.Ticks))
                    : TimeSpan.Zero
            })
            .OrderBy(h => h.Hour)
            .ToList();
    }

    private List<DailyHealthSummary> CalculateDailyBreakdown(List<HealthCheckRecord> records)
    {
        return records
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => new DailyHealthSummary
            {
                Date = g.Key,
                TotalChecks = g.Count(),
                FailedChecks = g.Count(r => r.Status.IsCritical()),
                AvailabilityPercentage = (double)g.Count(r => r.Status.IsHealthy()) / g.Count() * 100,
                HealthScore = CalculateHealthScore(g.ToList()),
                AverageResponseTime = g.Any(r => r.ResponseTime > TimeSpan.Zero)
                    ? TimeSpan.FromTicks((long)g.Where(r => r.ResponseTime > TimeSpan.Zero).Average(r => r.ResponseTime.Ticks))
                    : TimeSpan.Zero,
                MaxResponseTime = g.Any(r => r.ResponseTime > TimeSpan.Zero)
                    ? g.Max(r => r.ResponseTime)
                    : TimeSpan.Zero,
                CommonErrors = g.Where(r => r.Status.IsCritical() && !string.IsNullOrEmpty(r.ErrorMessage))
                    .GroupBy(r => r.ErrorMessage!)
                    .OrderByDescending(eg => eg.Count())
                    .Take(3)
                    .Select(eg => eg.Key)
                    .ToList()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private List<WeeklyHealthSummary> CalculateWeeklyBreakdown(List<HealthCheckRecord> records)
    {
        return records
            .GroupBy(r => GetWeekStart(r.Timestamp))
            .Select(g => new WeeklyHealthSummary
            {
                WeekStarting = g.Key,
                TotalChecks = g.Count(),
                FailedChecks = g.Count(r => r.Status == Status.Critical),
                AvailabilityPercentage = (double)g.Count(r => r.Status.IsHealthy()) / g.Count() * 100,
                HealthScore = CalculateHealthScore(g.ToList()),
                AverageResponseTime = g.Any(r => r.ResponseTime > TimeSpan.Zero)
                    ? TimeSpan.FromTicks((long)g.Where(r => r.ResponseTime > TimeSpan.Zero).Average(r => r.ResponseTime.Ticks))
                    : TimeSpan.Zero,
                WeekTrend = CalculateWeekTrend(g.OrderBy(r => r.Timestamp).ToList())
            })
            .OrderBy(w => w.WeekStarting)
            .ToList();
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private HealthTrendDirection CalculateHealthTrend(List<HealthCheckRecord> records)
    {
        if (records.Count < 10) return HealthTrendDirection.Unknown;

        // Split records into time segments and compare
        var totalTime = records.Max(r => r.Timestamp) - records.Min(r => r.Timestamp);
        var segmentDuration = TimeSpan.FromTicks(totalTime.Ticks / 4); // 4 segments
        
        var segments = new List<List<HealthCheckRecord>>();
        var startTime = records.Min(r => r.Timestamp);
        
        for (int i = 0; i < 4; i++)
        {
            var segmentStart = startTime.Add(TimeSpan.FromTicks(segmentDuration.Ticks * i));
            var segmentEnd = startTime.Add(TimeSpan.FromTicks(segmentDuration.Ticks * (i + 1)));
            var segmentRecords = records.Where(r => r.Timestamp >= segmentStart && r.Timestamp < segmentEnd).ToList();
            segments.Add(segmentRecords);
        }

        var healthScores = segments.Select(s => s.Any() ? CalculateHealthScore(s) : 0).ToList();
        
        // Calculate trend using linear regression
        var trend = CalculateLinearTrend(healthScores);
        
        if (Math.Abs(trend) < 1.0) return HealthTrendDirection.Stable;
        if (trend > 5.0) return HealthTrendDirection.Improving;
        if (trend < -5.0) return HealthTrendDirection.Declining;
        
        // Check for volatility
        var variance = CalculateVariance(healthScores);
        if (variance > 100) return HealthTrendDirection.Volatile;
        
        return trend > 0 ? HealthTrendDirection.Improving : HealthTrendDirection.Declining;
    }

    private HealthTrendDirection CalculateResponseTimeTrend(List<HealthCheckRecord> records)
    {
        var responseTimes = records.Where(r => r.ResponseTime > TimeSpan.Zero)
            .Select(r => r.ResponseTime.TotalMilliseconds)
            .ToList();

        if (responseTimes.Count < 10) return HealthTrendDirection.Unknown;

        // Calculate trend in response times over time
        var trend = CalculateLinearTrend(responseTimes);
        
        if (Math.Abs(trend) < 10) return HealthTrendDirection.Stable;
        
        // Negative trend means improving (decreasing response times)
        return trend < 0 ? HealthTrendDirection.Improving : HealthTrendDirection.Declining;
    }

    private double CalculateLinearTrend(List<double> values)
    {
        if (values.Count < 2) return 0;

        var n = values.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumXX = 0.0;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumXX += i * i;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        return slope;
    }

    private double CalculateVariance(List<double> values)
    {
        if (values.Count < 2) return 0;
        
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);
        return variance;
    }

    private double CalculateTrendConfidence(List<HealthCheckRecord> records)
    {
        if (records.Count < 50) return Math.Min(records.Count / 50.0, 1.0);
        
        // Higher confidence with more data points and consistent patterns
        var dataPointsFactor = Math.Min(records.Count / 1000.0, 1.0);
        
        // Calculate consistency of the data
        var hourlyBreakdown = CalculateHourlyBreakdown(records);
        var healthScoreVariance = CalculateVariance(hourlyBreakdown.Select(h => h.HealthScore).ToList());
        var consistencyFactor = Math.Max(0, 1.0 - healthScoreVariance / 100.0);
        
        return (dataPointsFactor + consistencyFactor) / 2.0;
    }

    private List<HealthAnomaly> DetectAnomalies(List<HealthCheckRecord> records)
    {
        var anomalies = new List<HealthAnomaly>();
        
        // Detect long outages
        anomalies.AddRange(DetectOutages(records));
        
        // Detect response time spikes
        anomalies.AddRange(DetectResponseTimeSpikes(records));
        
        // Detect unusual failure patterns
        anomalies.AddRange(DetectFailurePatterns(records));
        
        return anomalies.OrderBy(a => a.StartTime).ToList();
    }

    private List<HealthAnomaly> DetectOutages(List<HealthCheckRecord> records)
    {
        var anomalies = new List<HealthAnomaly>();
        var consecutiveFailures = 0;
        DateTime? outageStart = null;
        
        foreach (var record in records.OrderBy(r => r.Timestamp))
        {
            if (record.Status.IsCritical())
            {
                consecutiveFailures++;
                outageStart ??= record.Timestamp;
            }
            else
            {
                if (consecutiveFailures >= 5) // 5+ consecutive failures = outage
                {
                    anomalies.Add(new HealthAnomaly
                    {
                        Type = AnomalyType.LongOutage,
                        StartTime = outageStart!.Value,
                        EndTime = record.Timestamp,
                        Severity = Math.Min(consecutiveFailures / 60.0, 1.0), // Max severity at 60 failures (1 hour)
                        Description = $"Service outage lasting {consecutiveFailures} minutes",
                        Details = { ["consecutive_failures"] = consecutiveFailures }
                    });
                }
                consecutiveFailures = 0;
                outageStart = null;
            }
        }
        
        return anomalies;
    }

    private List<HealthAnomaly> DetectResponseTimeSpikes(List<HealthCheckRecord> records)
    {
        var anomalies = new List<HealthAnomaly>();
        var responseTimes = records.Where(r => r.ResponseTime > TimeSpan.Zero).ToList();
        
        if (responseTimes.Count < 10) return anomalies;
        
        var avgResponseTime = responseTimes.Average(r => r.ResponseTime.TotalMilliseconds);
        var threshold = avgResponseTime * 3; // 3x average is considered a spike
        
        var spikes = responseTimes.Where(r => r.ResponseTime.TotalMilliseconds > threshold).ToList();
        
        foreach (var spike in spikes)
        {
            anomalies.Add(new HealthAnomaly
            {
                Type = AnomalyType.HighResponseTime,
                StartTime = spike.Timestamp,
                EndTime = spike.Timestamp.AddMinutes(1),
                Severity = Math.Min(spike.ResponseTime.TotalMilliseconds / (avgResponseTime * 10), 1.0),
                Description = $"Response time spike: {spike.ResponseTime.TotalMilliseconds:F0}ms (avg: {avgResponseTime:F0}ms)",
                Details = { 
                    ["response_time"] = spike.ResponseTime.TotalMilliseconds,
                    ["average_response_time"] = avgResponseTime
                }
            });
        }
        
        return anomalies;
    }

    private List<HealthAnomaly> DetectFailurePatterns(List<HealthCheckRecord> records)
    {
        var anomalies = new List<HealthAnomaly>();
        
        // Group by hour and detect hours with unusual failure rates
        var hourlyGroups = records.GroupBy(r => r.Timestamp.Date.AddHours(r.Timestamp.Hour));
        var avgFailureRate = records.Count(r => r.Status.IsCritical()) / (double)records.Count;
        
        foreach (var hourGroup in hourlyGroups)
        {
            var hourRecords = hourGroup.ToList();
            var hourFailureRate = hourRecords.Count(r => r.Status.IsCritical()) / (double)hourRecords.Count;
            
            if (hourFailureRate > avgFailureRate * 3 && hourFailureRate > 0.1) // 3x average failure rate and >10%
            {
                anomalies.Add(new HealthAnomaly
                {
                    Type = AnomalyType.FrequentFailures,
                    StartTime = hourGroup.Key,
                    EndTime = hourGroup.Key.AddHours(1),
                    Severity = Math.Min(hourFailureRate / 0.5, 1.0), // Max severity at 50% failure rate
                    Description = $"High failure rate: {hourFailureRate:P1} (avg: {avgFailureRate:P1})",
                    Details = { 
                        ["failure_rate"] = hourFailureRate,
                        ["average_failure_rate"] = avgFailureRate,
                        ["failed_checks"] = hourRecords.Count(r => r.Status.IsCritical())
                    }
                });
            }
        }
        
        return anomalies;
    }

    private SlaMetrics CalculateSlaMetrics(List<HealthCheckRecord> records)
    {
        if (!records.Any()) return new SlaMetrics();

        var healthyRecords = records.Where(r => r.Status.IsHealthy()).ToList();
        var actualAvailability = (double)healthyRecords.Count / records.Count;
        
        var responseTimes = records.Where(r => r.ResponseTime > TimeSpan.Zero).ToList();
        var avgResponseTime = responseTimes.Any() 
            ? TimeSpan.FromTicks((long)responseTimes.Average(r => r.ResponseTime.Ticks))
            : TimeSpan.Zero;

        var slaMetrics = new SlaMetrics
        {
            ActualAvailability = actualAvailability,
            ActualAverageResponseTime = avgResponseTime,
            MeetingAvailabilitySla = actualAvailability >= 0.999, // 99.9%
            MeetingResponseTimeSla = avgResponseTime <= TimeSpan.FromSeconds(1)
        };

        // Calculate error budget
        var targetAvailability = slaMetrics.TargetAvailability;
        var allowedDowntime = (1 - targetAvailability) * records.Count;
        var actualDowntime = records.Count - healthyRecords.Count;
        slaMetrics.ErrorBudgetRemaining = Math.Max(0, (allowedDowntime - actualDowntime) / allowedDowntime);

        // Estimate monthly downtime
        var timeSpan = records.Max(r => r.Timestamp) - records.Min(r => r.Timestamp);
        var monthlyMultiplier = TimeSpan.FromDays(30).TotalMinutes / timeSpan.TotalMinutes;
        slaMetrics.EstimatedDowntimeThisMonth = TimeSpan.FromMinutes(actualDowntime * monthlyMultiplier);

        return slaMetrics;
    }

    private List<HealthPrediction> GeneratePredictions(List<HealthCheckRecord> records)
    {
        var predictions = new List<HealthPrediction>();
        
        if (records.Count < 100) return predictions; // Need sufficient data for predictions

        // Simple trend-based predictions for next 24 hours
        var recentTrend = CalculateRecentTrend(records);
        var currentHealth = CalculateHealthScore(records.TakeLast(60).ToList()); // Last hour
        
        for (int hours = 1; hours <= 24; hours++)
        {
            var predictedHealth = Math.Max(0, Math.Min(100, currentHealth + recentTrend * hours));
            var confidence = Math.Max(0, 1.0 - hours * 0.03); // Decreasing confidence over time
            
            predictions.Add(new HealthPrediction
            {
                PredictionTime = DateTime.UtcNow.AddHours(hours),
                PredictedHealthScore = predictedHealth,
                Confidence = confidence,
                ReasoningFactors = $"Based on recent {recentTrend:F1} health trend"
            });
        }

        return predictions;
    }

    private double CalculateRecentTrend(List<HealthCheckRecord> records)
    {
        // Calculate trend over last 6 hours
        var recentRecords = records.Where(r => r.Timestamp > DateTime.UtcNow.AddHours(-6)).ToList();
        if (recentRecords.Count < 10) return 0;

        var hourlyScores = recentRecords
            .GroupBy(r => r.Timestamp.Date.AddHours(r.Timestamp.Hour))
            .Select(g => CalculateHealthScore(g.ToList()))
            .ToList();

        return CalculateLinearTrend(hourlyScores);
    }

    private HealthTrendDirection CalculateWeekTrend(List<HealthCheckRecord> weekRecords)
    {
        if (weekRecords.Count < 50) return HealthTrendDirection.Unknown;

        var dailyScores = weekRecords
            .GroupBy(r => r.Timestamp.Date)
            .Select(g => CalculateHealthScore(g.ToList()))
            .ToList();

        var trend = CalculateLinearTrend(dailyScores);
        
        if (Math.Abs(trend) < 2) return HealthTrendDirection.Stable;
        return trend > 0 ? HealthTrendDirection.Improving : HealthTrendDirection.Declining;
    }
}