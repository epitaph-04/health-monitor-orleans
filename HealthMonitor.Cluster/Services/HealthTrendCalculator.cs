using HealthMonitor.Model;

namespace HealthMonitor.Cluster.Services;

public class HealthTrendCalculator
{
    public ValueTask<HealthTrendData> CalculateTrend(
        string serviceId,
        List<HealthCheckRecord> records,
        TimeSpan analysisWindow,
        List<HealthTrendData>? trendHistory = null)
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
        
        // Calculate trends with improved algorithms using trend history
        trendData.HealthTrend = CalculateHealthTrend(records, trendHistory);
        trendData.ResponseTimeTrend = CalculateResponseTimeTrend(records, trendHistory);
        trendData.TrendConfidence = CalculateTrendConfidence(records, trendHistory);
        
        // Detect anomalies
        trendData.DetectedAnomalies = DetectAnomalies(records);
        
        // Calculate SLA metrics
        trendData.SlaMetrics = CalculateSlaMetrics(records);
        
        // Generate predictions using trend history for better accuracy
        trendData.Predictions = GeneratePredictions(records, trendHistory);

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

    private HealthTrendDirection CalculateHealthTrend(List<HealthCheckRecord> records, List<HealthTrendData>? trendHistory = null)
    {
        if (records.Count < 10) return HealthTrendDirection.Unknown;

        // Use trend history for better trend analysis if available
        if (trendHistory?.Count >= 5)
        {
            return CalculateTrendFromHistory(trendHistory);
        }

        // Fallback to current records analysis
        return CalculateTrendFromRecords(records);
    }

    private HealthTrendDirection CalculateTrendFromHistory(List<HealthTrendData> trendHistory)
    {
        // Use the last 10 trend calculations for more stable trend detection
        var recentTrends = trendHistory
            .OrderByDescending(t => t.CalculatedAt)
            .Take(10)
            .OrderBy(t => t.CalculatedAt)
            .ToList();

        if (recentTrends.Count < 3) return HealthTrendDirection.Unknown;

        var healthScores = recentTrends.Select(t => t.OverallHealthScore).ToList();
        var trend = CalculateLinearTrend(healthScores);
        var variance = CalculateVariance(healthScores);

        // More nuanced thresholds based on historical data
        if (variance > 200) return HealthTrendDirection.Volatile;
        if (Math.Abs(trend) < 0.5) return HealthTrendDirection.Stable;
        if (trend > 2.0) return HealthTrendDirection.Improving;
        if (trend < -2.0) return HealthTrendDirection.Declining;

        return trend > 0 ? HealthTrendDirection.Improving : HealthTrendDirection.Declining;
    }

    private HealthTrendDirection CalculateTrendFromRecords(List<HealthCheckRecord> records)
    {
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

    private HealthTrendDirection CalculateResponseTimeTrend(List<HealthCheckRecord> records, List<HealthTrendData>? trendHistory = null)
    {
        // Use trend history for better response time trend analysis if available
        if (trendHistory?.Count >= 5)
        {
            var recentTrends = trendHistory
                .OrderByDescending(t => t.CalculatedAt)
                .Take(8)
                .OrderBy(t => t.CalculatedAt)
                .ToList();

            var avgResponseTimes = recentTrends.Select(t => t.AverageResponseTime.TotalMilliseconds).ToList();
            var responseTrend = CalculateLinearTrend(avgResponseTimes);

            if (Math.Abs(responseTrend) < 5) return HealthTrendDirection.Stable;
            // Negative trend means improving (decreasing response times)
            return responseTrend < 0 ? HealthTrendDirection.Improving : HealthTrendDirection.Declining;
        }

        // Fallback to current records
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

    private double CalculateTrendConfidence(List<HealthCheckRecord> records, List<HealthTrendData>? trendHistory = null)
    {
        if (records.Count < 50) return Math.Min(records.Count / 50.0, 1.0);

        // Base confidence from current data
        var dataPointsFactor = Math.Min(records.Count / 1000.0, 1.0);

        // Calculate consistency of the current data
        var hourlyBreakdown = CalculateHourlyBreakdown(records);
        var healthScoreVariance = CalculateVariance(hourlyBreakdown.Select(h => h.HealthScore).ToList());
        var consistencyFactor = Math.Max(0, 1.0 - healthScoreVariance / 100.0);

        var baseConfidence = (dataPointsFactor + consistencyFactor) / 2.0;

        // Enhance confidence with trend history if available
        if (trendHistory?.Count >= 10)
        {
            var historyConfidenceFactor = CalculateHistoryConfidenceFactor(trendHistory);
            return (baseConfidence + historyConfidenceFactor) / 2.0;
        }

        return baseConfidence;
    }

    private double CalculateHistoryConfidenceFactor(List<HealthTrendData> trendHistory)
    {
        // More trend history = higher confidence in predictions
        var historyLengthFactor = Math.Min(trendHistory.Count / 50.0, 1.0);

        // Consistent trend direction = higher confidence
        var recentTrends = trendHistory.TakeLast(10).ToList();
        var trendDirections = recentTrends.Select(t => t.HealthTrend).ToList();
        var mostCommonTrend = trendDirections.GroupBy(t => t).OrderByDescending(g => g.Count()).First();
        var trendConsistency = mostCommonTrend.Count() / (double)trendDirections.Count;

        // Lower variance in historical confidence = higher overall confidence
        var historicalConfidences = recentTrends.Select(t => t.TrendConfidence).ToList();
        var confidenceVariance = CalculateVariance(historicalConfidences);
        var confidenceStability = Math.Max(0, 1.0 - confidenceVariance);

        return (historyLengthFactor + trendConsistency + confidenceStability) / 3.0;
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
        var monthlyMultiplier = (int)(TimeSpan.FromDays(30).TotalMinutes / timeSpan.TotalMinutes);
        slaMetrics.EstimatedDowntimeThisMonth = TimeSpan.FromMinutes(actualDowntime * monthlyMultiplier);

        return slaMetrics;
    }

    private List<HealthPrediction> GeneratePredictions(List<HealthCheckRecord> records, List<HealthTrendData>? trendHistory = null)
    {
        var predictions = new List<HealthPrediction>();

        if (records.Count < 100) return predictions; // Need sufficient data for predictions

        // Use trend history for more sophisticated predictions if available
        if (trendHistory?.Count >= 10)
        {
            return GeneratePredictionsFromHistory(trendHistory, records);
        }

        // Fallback to simple trend-based predictions
        return GenerateSimplePredictions(records);
    }

    private List<HealthPrediction> GeneratePredictionsFromHistory(List<HealthTrendData> trendHistory, List<HealthCheckRecord> currentRecords)
    {
        var predictions = new List<HealthPrediction>();
        var recentTrends = trendHistory.OrderByDescending(t => t.CalculatedAt).Take(15).OrderBy(t => t.CalculatedAt).ToList();

        // Calculate multiple trend patterns
        var healthScores = recentTrends.Select(t => t.OverallHealthScore).ToList();
        var shortTermTrend = CalculateLinearTrend(healthScores.TakeLast(5).ToList());
        var longTermTrend = CalculateLinearTrend(healthScores);

        var currentHealth = CalculateHealthScore(currentRecords.TakeLast(60).ToList());

        // Detect cyclical patterns (daily, weekly)
        var cyclicalFactor = DetectCyclicalPatterns(recentTrends);

        for (int hours = 1; hours <= 24; hours++)
        {
            // Weighted combination of short-term and long-term trends
            var trendWeight = Math.Exp(-hours / 12.0); // Decay weight over time
            var combinedTrend = (shortTermTrend * trendWeight) + (longTermTrend * (1 - trendWeight));

            // Apply cyclical adjustment
            var cyclicalAdjustment = cyclicalFactor * Math.Sin(2 * Math.PI * hours / 24.0) * 2; // Daily cycle

            var predictedHealth = Math.Max(0, Math.Min(100, currentHealth + combinedTrend * hours + cyclicalAdjustment));

            // Confidence decreases over time but is higher with more trend history
            var baseConfidence = Math.Min(trendHistory.Count / 20.0, 0.9);
            var timeDecay = Math.Max(0, 1.0 - hours * 0.02); // Slower decay with history
            var confidence = baseConfidence * timeDecay;

            var reasoningFactors = $"Historical trend analysis: short-term {shortTermTrend:F1}, long-term {longTermTrend:F1}";
            if (Math.Abs(cyclicalAdjustment) > 0.5)
                reasoningFactors += $", cyclical pattern detected";

            predictions.Add(new HealthPrediction
            {
                PredictionTime = DateTime.UtcNow.AddHours(hours),
                PredictedHealthScore = predictedHealth,
                Confidence = confidence,
                ReasoningFactors = reasoningFactors
            });
        }

        return predictions;
    }

    private List<HealthPrediction> GenerateSimplePredictions(List<HealthCheckRecord> records)
    {
        var predictions = new List<HealthPrediction>();

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

    private double DetectCyclicalPatterns(List<HealthTrendData> trendHistory)
    {
        if (trendHistory.Count < 10) return 0;

        // Simple cyclical pattern detection based on time of day
        var hourlyPattern = new double[24];
        var hourlyCounts = new int[24];

        foreach (var trend in trendHistory)
        {
            var hour = trend.CalculatedAt.Hour;
            hourlyPattern[hour] += trend.OverallHealthScore;
            hourlyCounts[hour]++;
        }

        // Calculate average health score for each hour
        for (int i = 0; i < 24; i++)
        {
            if (hourlyCounts[i] > 0)
                hourlyPattern[i] /= hourlyCounts[i];
        }

        // Return the variance in hourly patterns (indicator of cyclical behavior)
        var variance = CalculateVariance(hourlyPattern.Where(h => h > 0).ToList());
        return Math.Min(variance / 100.0, 5.0); // Cap the cyclical factor
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