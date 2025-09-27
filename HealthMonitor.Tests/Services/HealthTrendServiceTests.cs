using HealthMonitor.Services;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace HealthMonitor.Tests.Services;

public class HealthTrendServiceTests
{
    private readonly IGrainFactory _mockGrainFactory;
    private readonly ILogger<HealthTrendService> _mockLogger;
    private readonly HealthTrendService _service;

    public HealthTrendServiceTests()
    {
        _mockGrainFactory = Substitute.For<IGrainFactory>();
        _mockLogger = Substitute.For<ILogger<HealthTrendService>>();
        _service = new HealthTrendService(_mockGrainFactory, _mockLogger);
    }

    [Fact]
    public async Task GetServiceTrend_ValidService_ShouldReturnTrend()
    {
        // Arrange
        var serviceId = "test-service";
        var hours = 24;
        var expectedTrend = new HealthTrendData
        {
            ServiceId = serviceId,
            TimeWindow = TimeSpan.FromHours(hours),
            CalculatedAt = DateTime.UtcNow
        };

        var mockTrendGrain = Substitute.For<IHealthTrendGrain>();
        mockTrendGrain.CalculateTrend(TimeSpan.FromHours(hours), Arg.Any<CancellationToken>())
                      .Returns(expectedTrend);

        _mockGrainFactory.GetGrain<IHealthTrendGrain>(serviceId)
                        .Returns(mockTrendGrain);

        // Act
        var result = await _service.GetServiceTrend(serviceId, hours, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(TimeSpan.FromHours(hours), result.TimeWindow);
    }

    [Fact]
    public async Task GetServiceTrend_GrainThrowsException_ShouldReturnFallbackTrend()
    {
        // Arrange
        var serviceId = "failing-service";
        var hours = 24;

        var mockTrendGrain = Substitute.For<IHealthTrendGrain>();
        mockTrendGrain.When(x => x.CalculateTrend(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()))
                      .Do(_ => throw new Exception("Grain failure"));

        _mockGrainFactory.GetGrain<IHealthTrendGrain>(serviceId)
                        .Returns(mockTrendGrain);

        // Act
        var result = await _service.GetServiceTrend(serviceId, hours, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(TimeSpan.FromHours(hours), result.TimeWindow);
        Assert.Equal(HealthTrendDirection.Unknown, result.HealthTrend);
        Assert.Equal(HealthTrendDirection.Unknown, result.ResponseTimeTrend);
    }

    [Fact]
    public async Task GetServiceTrendHistory_ValidService_ShouldReturnHistory()
    {
        // Arrange
        var serviceId = "test-service";
        var count = 10;
        var expectedHistory = new List<HealthTrendData>
        {
            new() { ServiceId = serviceId, CalculatedAt = DateTime.UtcNow.AddHours(-1) },
            new() { ServiceId = serviceId, CalculatedAt = DateTime.UtcNow.AddHours(-2) }
        };

        var mockTrendGrain = Substitute.For<IHealthTrendGrain>();
        mockTrendGrain.GetTrendHistory(count, Arg.Any<CancellationToken>())
                      .Returns(expectedHistory);

        _mockGrainFactory.GetGrain<IHealthTrendGrain>(serviceId)
                        .Returns(mockTrendGrain);

        // Act
        var result = await _service.GetServiceTrendHistory(serviceId, count, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, trend => Assert.Equal(serviceId, trend.ServiceId));
    }

    [Fact]
    public async Task GetServiceTrendHistory_GrainThrowsException_ShouldReturnEmptyList()
    {
        // Arrange
        var serviceId = "failing-service";
        var count = 10;

        var mockTrendGrain = Substitute.For<IHealthTrendGrain>();
        mockTrendGrain.When(x => x.GetTrendHistory(Arg.Any<int>(), Arg.Any<CancellationToken>()))
                      .Do(_ => throw new Exception("Grain failure"));

        _mockGrainFactory.GetGrain<IHealthTrendGrain>(serviceId)
                        .Returns(mockTrendGrain);

        // Act
        var result = await _service.GetServiceTrendHistory(serviceId, count, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSystemOverview_ValidCall_ShouldReturnOverview()
    {
        // Arrange
        var expectedOverview = new SystemHealthOverview
        {
            GeneratedAt = DateTime.UtcNow,
            TotalServices = 5,
            HealthyServices = 4,
            ProblematicServices = 1
        };

        var mockAggregatorGrain = Substitute.For<IHealthTrendAggregatorGrain>();
        mockAggregatorGrain.GetSystemOverview(Arg.Any<CancellationToken>())
                          .Returns(expectedOverview);

        _mockGrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system")
                        .Returns(mockAggregatorGrain);

        // Act
        var result = await _service.GetSystemOverview(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalServices);
        Assert.Equal(4, result.HealthyServices);
        Assert.Equal(1, result.ProblematicServices);
    }

    [Fact]
    public async Task GetSystemOverview_GrainThrowsException_ShouldReturnFallbackOverview()
    {
        // Arrange
        var mockAggregatorGrain = Substitute.For<IHealthTrendAggregatorGrain>();
        mockAggregatorGrain.When(x => x.GetSystemOverview(Arg.Any<CancellationToken>()))
                          .Do(_ => throw new Exception("Aggregator failure"));

        _mockGrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system")
                        .Returns(mockAggregatorGrain);

        // Act
        var result = await _service.GetSystemOverview(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalServices);
        Assert.Equal(0, result.HealthyServices);
        Assert.Equal(0, result.ProblematicServices);
        Assert.NotNull(result.Alerts);
        Assert.NotNull(result.ServicesByTrend);
    }

    [Fact]
    public async Task CompareServices_ValidRequest_ShouldReturnComparison()
    {
        // Arrange
        var request = new CompareServicesRequest
        {
            ServiceIds = ["service1", "service2"],
            Hours = 24
        };

        var expectedComparison = new HealthTrendComparisonReport
        {
            AnalysisWindow = TimeSpan.FromHours(24),
            ServiceComparisons = [],
            HealthRanking = new SystemHealthRanking()
        };

        var mockAggregatorGrain = Substitute.For<IHealthTrendAggregatorGrain>();
        mockAggregatorGrain.CompareServiceTrends(request.ServiceIds, TimeSpan.FromHours(request.Hours), Arg.Any<CancellationToken>())
                          .Returns(expectedComparison);

        _mockGrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system")
                        .Returns(mockAggregatorGrain);

        // Act
        var result = await _service.CompareServices(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromHours(24), result.AnalysisWindow);
        Assert.NotNull(result.ServiceComparisons);
        Assert.NotNull(result.HealthRanking);
    }

    [Fact]
    public async Task CompareServices_EmptyServiceIds_ShouldReturnEmptyComparison()
    {
        // Arrange
        var request = new CompareServicesRequest
        {
            ServiceIds = [],
            Hours = 24
        };

        // Act
        var result = await _service.CompareServices(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromHours(24), result.AnalysisWindow);
        Assert.NotNull(result.ServiceComparisons);
        Assert.Empty(result.ServiceComparisons);
        Assert.NotNull(result.HealthRanking);
    }

    [Fact]
    public async Task RefreshAllTrends_ValidCall_ShouldSucceed()
    {
        // Arrange
        var mockAggregatorGrain = Substitute.For<IHealthTrendAggregatorGrain>();
        mockAggregatorGrain.RefreshAllTrends(Arg.Any<CancellationToken>())
                          .Returns(ValueTask.CompletedTask);

        _mockGrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system")
                        .Returns(mockAggregatorGrain);

        // Act & Assert - Should not throw
        await _service.RefreshAllTrends(CancellationToken.None);

        // Verify the grain method was called
        await mockAggregatorGrain.Received(1).RefreshAllTrends(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAllTrends_GrainThrowsException_ShouldRethrowException()
    {
        // Arrange
        var mockAggregatorGrain = Substitute.For<IHealthTrendAggregatorGrain>();
        mockAggregatorGrain.When(x => x.RefreshAllTrends(Arg.Any<CancellationToken>()))
                          .Do(_ => throw new Exception("Refresh failure"));

        _mockGrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system")
                        .Returns(mockAggregatorGrain);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.RefreshAllTrends(CancellationToken.None));
    }
}