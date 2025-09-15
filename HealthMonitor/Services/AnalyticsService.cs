using HealthMonitor.Model;

namespace HealthMonitor.Services;

public class AnalyticsService(ApplicationServices applicationServices)
{
    public async Task<HealthTrendData> GenerateHealthTrend(TimeSpan period)
    {
        var services = applicationServices.GetServicesMetadata().Select(metadata => metadata.Id).ToArray();
        var dataPoints = new List<HealthTrendPoint>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-period.TotalDays);
        
        // Generate hourly data points for the last 7 days, or daily for longer periods
        var interval = period.TotalDays <= 7 ? TimeSpan.FromHours(1) : TimeSpan.FromDays(1);
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            // Simulate health data for each time point
            var random = new Random((int)currentDate.Ticks);
            var totalServices = services.Length;
            var healthyCount = (int)(totalServices * (0.85 + random.NextDouble() * 0.15)); // 85-100% healthy
            var degradedCount = (int)(totalServices * (0.0 + random.NextDouble() * 0.1)); // 0-10% degraded
            var criticalCount = totalServices - healthyCount - degradedCount;
            
            var healthScore = totalServices > 0 ? (double)healthyCount / totalServices * 100 : 0;
            
            dataPoints.Add(new HealthTrendPoint
            {
                Timestamp = currentDate,
                HealthScore = healthScore,
                HealthyServices = healthyCount,
                DegradedServices = degradedCount,
                CriticalServices = criticalCount,
                TotalServices = totalServices
            });
            
            currentDate = currentDate.Add(interval);
        }
        
        var currentHealthScore = dataPoints.LastOrDefault()?.HealthScore ?? 0;
        var averageHealthScore = dataPoints.Any() ? dataPoints.Average(p => p.HealthScore) : 0;
        
        // Calculate trend
        var recentPoints = dataPoints.TakeLast(Math.Min(24, dataPoints.Count)).ToArray();
        var trend = CalculateHealthTrend(recentPoints.Select(p => p.HealthScore).ToArray());
        
        return new HealthTrendData
        {
            Period = period,
            DataPoints = dataPoints.ToArray(),
            CurrentHealthScore = currentHealthScore,
            AverageHealthScore = averageHealthScore,
            Trend = trend.Direction,
            TrendStrength = trend.Strength,
            ImprovingServices = services.Take(3).ToArray(),
            DegradingServices = services.Skip(Math.Max(0, services.Length - 2)).ToArray()
        };
    }

    private (TrendDirection Direction, double Strength) CalculateHealthTrend(double[] values)
    {
        if (values.Length < 2) return (TrendDirection.Stable, 0);
        
        var firstHalf = values.Take(values.Length / 2).Average();
        var secondHalf = values.Skip(values.Length / 2).Average();
        var change = secondHalf - firstHalf;
        var strength = Math.Abs(change) / Math.Max(firstHalf, 1);
        
        return change switch
        {
            > 2 => (TrendDirection.Increasing, strength),
            < -2 => (TrendDirection.Decreasing, strength),
            _ => (TrendDirection.Stable, strength)
        };
    }
}