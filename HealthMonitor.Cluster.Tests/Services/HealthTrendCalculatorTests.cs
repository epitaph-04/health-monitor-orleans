using HealthMonitor.Cluster.Services;
using HealthMonitor.Model;

namespace HealthMonitor.Cluster.Tests.Services;

public class HealthTrendCalculatorTests
{
    private readonly HealthTrendCalculator _calculator = new();

    [Fact]
    public async Task CalculateTrend_WithNoRecords_ShouldReturnEmptyTrend()
    {
        // Arrange
        var serviceId = "test-service";
        var records = new List<HealthCheckRecord>();
        var analysisWindow = TimeSpan.FromHours(24);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(analysisWindow, result.TimeWindow);
        Assert.Equal(0, result.TotalDataPoints);
        Assert.Equal(0, result.AvailabilityPercentage);
        Assert.Equal(0, result.OverallHealthScore);
    }

    [Fact]
    public async Task CalculateTrend_WithAllHealthyRecords_ShouldReturn100PercentAvailability()
    {
        // Arrange
        var serviceId = "test-service";
        var records = GenerateHealthyRecords(10);
        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.Equal(100, result.AvailabilityPercentage);
        Assert.Equal(10, result.TotalDataPoints);
        Assert.True(result.OverallHealthScore > 90);
    }

    [Fact]
    public async Task CalculateTrend_WithMixedRecords_ShouldCalculateCorrectAvailability()
    {
        // Arrange
        var serviceId = "test-service";
        var records = new List<HealthCheckRecord>();
        records.AddRange(GenerateHealthyRecords(7)); // 70% healthy
        records.AddRange(GenerateCriticalRecords(3)); // 30% critical
        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.Equal(70, result.AvailabilityPercentage);
        Assert.Equal(10, result.TotalDataPoints);
        Assert.True(result.OverallHealthScore < 100);
    }

    [Fact]
    public async Task CalculateTrend_WithTrendHistory_ShouldUseBetterPredictions()
    {
        // Arrange
        var serviceId = "test-service";
        var records = GenerateHealthyRecords(100);
        var analysisWindow = TimeSpan.FromHours(24);
        var trendHistory = GenerateTrendHistory(10, serviceId);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow, trendHistory);

        // Assert
        Assert.True(result.TrendConfidence > 0.5);
        Assert.NotEmpty(result.Predictions);
        Assert.Equal(24, result.Predictions.Count); // 24 hours of predictions
    }

    [Fact]
    public async Task CalculateTrend_ShouldDetectOutageAnomalies()
    {
        // Arrange
        var serviceId = "test-service";
        var records = new List<HealthCheckRecord>();
        var baseTime = DateTime.UtcNow.AddHours(-1);

        // Generate 5 healthy records
        for (int i = 0; i < 5; i++)
        {
            records.Add(new HealthCheckRecord
            {
                Timestamp = baseTime.AddMinutes(i),
                Status = Status.Healthy,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            });
        }

        // Generate 10 critical records (long outage)
        for (int i = 5; i < 15; i++)
        {
            records.Add(new HealthCheckRecord
            {
                Timestamp = baseTime.AddMinutes(i),
                Status = Status.Critical,
                ErrorMessage = "Service unavailable",
                ResponseTime = TimeSpan.Zero
            });
        }

        // Generate 5 more healthy records
        for (int i = 15; i < 20; i++)
        {
            records.Add(new HealthCheckRecord
            {
                Timestamp = baseTime.AddMinutes(i),
                Status = Status.Healthy,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            });
        }

        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.NotEmpty(result.DetectedAnomalies);
        Assert.Contains(result.DetectedAnomalies, a => a.Type == AnomalyType.LongOutage);
    }

    [Fact]
    public async Task CalculateTrend_ShouldDetectResponseTimeSpikes()
    {
        // Arrange
        var serviceId = "test-service";
        var records = GenerateRecordsWithResponseTimes(20);
        records.Add(new HealthCheckRecord
        {
            Timestamp = DateTime.UtcNow,
            Status = Status.Healthy,
            ResponseTime = TimeSpan.FromSeconds(10) // Spike compared to normal 100ms
        });
        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.Contains(result.DetectedAnomalies, a => a.Type == AnomalyType.HighResponseTime);
    }

    [Fact]
    public async Task CalculateTrend_ShouldCalculateHourlyBreakdown()
    {
        // Arrange
        var serviceId = "test-service";
        var records = GenerateRecordsAcrossHours(48);
        var analysisWindow = TimeSpan.FromDays(2);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.NotEmpty(result.HourlyBreakdown);
        Assert.NotEmpty(result.DailyBreakdown);
        Assert.NotEmpty(result.WeeklyBreakdown);
    }

    [Fact]
    public async Task CalculateTrend_ShouldCalculateSlaMetrics()
    {
        // Arrange
        var serviceId = "test-service";
        var records = new List<HealthCheckRecord>();
        records.AddRange(GenerateHealthyRecords(999)); // 99.9% availability
        records.AddRange(GenerateCriticalRecords(1));
        var analysisWindow = TimeSpan.FromDays(1);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.NotNull(result.SlaMetrics);
        Assert.InRange(result.SlaMetrics.ActualAvailability, 0.998, 1.0);
        Assert.True(result.SlaMetrics.MeetingAvailabilitySla);
    }

    [Theory]
    [InlineData(5, HealthTrendDirection.Unknown)] // Too few data points
    [InlineData(50, HealthTrendDirection.Stable)] // Stable data
    public async Task CalculateTrend_ShouldDetermineCorrectTrendDirection(int recordCount, HealthTrendDirection expectedTrend)
    {
        // Arrange
        var serviceId = "test-service";
        var records = GenerateHealthyRecords(recordCount);
        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result = await _calculator.CalculateTrend(serviceId, records, analysisWindow);

        // Assert
        Assert.Equal(expectedTrend, result.HealthTrend);
    }

    private List<HealthCheckRecord> GenerateHealthyRecords(int count)
    {
        var records = new List<HealthCheckRecord>();
        var baseTime = DateTime.UtcNow.AddHours(-1);

        for (int i = 0; i < count; i++)
        {
            records.Add(new HealthCheckRecord
            {
                Timestamp = baseTime.AddMinutes(i),
                Status = Status.Healthy,
                ResponseTime = TimeSpan.FromMilliseconds(100 + Random.Shared.Next(0, 50))
            });
        }

        return records;
    }

    private List<HealthCheckRecord> GenerateCriticalRecords(int count)
    {
        var records = new List<HealthCheckRecord>();
        var baseTime = DateTime.UtcNow.AddHours(-1);

        for (int i = 0; i < count; i++)
        {
            records.Add(new HealthCheckRecord
            {
                Timestamp = baseTime.AddMinutes(i),
                Status = Status.Critical,
                ErrorMessage = "Service unavailable",
                ResponseTime = TimeSpan.Zero
            });
        }

        return records;
    }

    private List<HealthCheckRecord> GenerateRecordsWithResponseTimes(int count)
    {
        var records = new List<HealthCheckRecord>();
        var baseTime = DateTime.UtcNow.AddHours(-1);

        for (int i = 0; i < count; i++)
        {
            records.Add(new HealthCheckRecord
            {
                Timestamp = baseTime.AddMinutes(i),
                Status = Status.Healthy,
                ResponseTime = TimeSpan.FromMilliseconds(100) // Normal response time
            });
        }

        return records;
    }

    private List<HealthCheckRecord> GenerateRecordsAcrossHours(int hours)
    {
        var records = new List<HealthCheckRecord>();
        var baseTime = DateTime.UtcNow.AddHours(-hours);

        for (int i = 0; i < hours; i++)
        {
            for (int j = 0; j < 4; j++) // 4 records per hour
            {
                records.Add(new HealthCheckRecord
                {
                        Timestamp = baseTime.AddHours(i).AddMinutes(j * 15),
                    Status = Status.Healthy,
                    ResponseTime = TimeSpan.FromMilliseconds(100)
                });
            }
        }

        return records;
    }

    private List<HealthTrendData> GenerateTrendHistory(int count, string serviceId)
    {
        var history = new List<HealthTrendData>();
        var baseTime = DateTime.UtcNow.AddDays(-count);

        for (int i = 0; i < count; i++)
        {
            history.Add(new HealthTrendData
            {
                ServiceId = serviceId,
                CalculatedAt = baseTime.AddDays(i),
                TimeWindow = TimeSpan.FromHours(24),
                OverallHealthScore = 90 + Random.Shared.Next(-10, 10),
                AvailabilityPercentage = 95 + Random.Shared.Next(-5, 5),
                HealthTrend = HealthTrendDirection.Stable,
                TrendConfidence = 0.8
            });
        }

        return history;
    }
}