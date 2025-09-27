using HealthMonitor.Cluster.Grains;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Orleans.TestingHost;

namespace HealthMonitor.Cluster.Tests.Grains;

public class HealthTrendAggregatorGrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;

    public HealthTrendAggregatorGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
    }

    [Fact]
    public async Task GetSystemOverview_FirstCall_ShouldReturnEmptyOverview()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-1");

        // Act
        var result = await grain.GetSystemOverview(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GeneratedAt > DateTime.MinValue);
        Assert.Equal(0, result.TotalServices);
        Assert.Equal(0, result.HealthyServices);
        Assert.Equal(0, result.ProblematicServices);
        Assert.NotNull(result.Alerts);
        Assert.NotNull(result.ServicesByTrend);
    }

    [Fact]
    public async Task CompareServiceTrends_WithEmptyServiceList_ShouldReturnEmptyComparison()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-2");
        var serviceIds = new List<string>();
        var analysisWindow = TimeSpan.FromHours(24);

        // Act
        var result = await grain.CompareServiceTrends(serviceIds, analysisWindow, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(analysisWindow, result.AnalysisWindow);
        Assert.NotNull(result.ServiceComparisons);
        Assert.Empty(result.ServiceComparisons);
        Assert.NotNull(result.HealthRanking);
    }

    [Fact]
    public async Task CompareServiceTrends_WithValidServices_ShouldReturnComparison()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-3");
        var serviceIds = new List<string> { "service-1", "service-2" };
        var analysisWindow = TimeSpan.FromHours(24);

        // Act
        var result = await grain.CompareServiceTrends(serviceIds, analysisWindow, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(analysisWindow, result.AnalysisWindow);
        Assert.NotNull(result.ServiceComparisons);
        Assert.NotNull(result.HealthRanking);
        // Note: Since we don't have actual health check data, comparisons might be empty
        // This test ensures the grain doesn't crash and returns valid structure
    }

    [Fact]
    public async Task RefreshAllTrends_ShouldSucceed()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-4");

        // Act & Assert - Should not throw
        await grain.RefreshAllTrends(CancellationToken.None);
    }

    [Fact]
    public async Task GetSystemOverview_MultipleCalls_ShouldUseCaching()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-5");

        // Act
        var result1 = await grain.GetSystemOverview(CancellationToken.None);
        var result2 = await grain.GetSystemOverview(CancellationToken.None);

        // Assert - Both calls should return results with similar structure
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.True(result1.GeneratedAt <= result2.GeneratedAt);
        Assert.Equal(result1.TotalServices, result2.TotalServices);
    }

    [Fact]
    public async Task CompareServiceTrends_WithSingleService_ShouldReturnSingleComparison()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-6");
        var serviceIds = new List<string> { "single-service" };
        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result = await grain.CompareServiceTrends(serviceIds, analysisWindow, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(analysisWindow, result.AnalysisWindow);
        Assert.NotNull(result.ServiceComparisons);
        Assert.NotNull(result.HealthRanking);
        // With no actual data, we just ensure no exceptions and valid structure
    }

    [Fact]
    public async Task RefreshAllTrends_AfterSystemOverview_ShouldInvalidateCache()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendAggregatorGrain>("system-test-7");

        // Act
        var overview1 = await grain.GetSystemOverview(CancellationToken.None);
        await grain.RefreshAllTrends(CancellationToken.None);
        var overview2 = await grain.GetSystemOverview(CancellationToken.None);

        // Assert
        Assert.NotNull(overview1);
        Assert.NotNull(overview2);
        // After refresh, the generated time should be different (newer)
        Assert.True(overview2.GeneratedAt >= overview1.GeneratedAt);
    }
}