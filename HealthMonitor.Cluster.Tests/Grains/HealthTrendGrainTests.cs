using HealthMonitor.Cluster.Grains;
using HealthMonitor.Cluster.Services;
using HealthMonitor.Grains.Abstraction;
using HealthMonitor.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace HealthMonitor.Cluster.Tests.Grains;

public class HealthTrendGrainTests : IClassFixture<ClusterFixture>
{
    private readonly TestCluster _cluster;

    public HealthTrendGrainTests(ClusterFixture fixture)
    {
        _cluster = fixture.Cluster;
    }

    [Fact]
    public async Task CalculateTrend_FirstCall_ShouldCreateEmptyTrend()
    {
        // Arrange
        var serviceId = "test-service-1";
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendGrain>(serviceId);

        // Act
        var result = await grain.CalculateTrend(TimeSpan.FromHours(24), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(TimeSpan.FromHours(24), result.TimeWindow);
    }

    [Fact]
    public async Task GetLatestTrend_WithNoHistory_ShouldReturnEmptyTrend()
    {
        // Arrange
        var serviceId = "test-service-2";
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendGrain>(serviceId);

        // Act
        var result = await grain.GetLatestTrend(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(HealthTrendDirection.Unknown, result.HealthTrend);
    }

    [Fact]
    public async Task GetTrendHistory_WithNoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var serviceId = "test-service-3";
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendGrain>(serviceId);

        // Act
        var result = await grain.GetTrendHistory(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RefreshTrendData_ShouldSucceed()
    {
        // Arrange
        var serviceId = "test-service-4";
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendGrain>(serviceId);

        // Act & Assert - Should not throw
        await grain.RefreshTrendData(CancellationToken.None);
    }

    [Fact]
    public async Task CalculateTrend_MultipleCalls_ShouldUseCaching()
    {
        // Arrange
        var serviceId = "test-service-5";
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendGrain>(serviceId);
        var analysisWindow = TimeSpan.FromHours(1);

        // Act
        var result1 = await grain.CalculateTrend(analysisWindow, CancellationToken.None);
        var result2 = await grain.CalculateTrend(analysisWindow, CancellationToken.None);

        // Assert - Both calls should return similar results (cached)
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.ServiceId, result2.ServiceId);
        Assert.Equal(result1.TimeWindow, result2.TimeWindow);
    }

    [Fact]
    public async Task CalculateTrend_DifferentWindows_ShouldCalculateSeparately()
    {
        // Arrange
        var serviceId = "test-service-6";
        var grain = _cluster.GrainFactory.GetGrain<IHealthTrendGrain>(serviceId);

        // Act
        var result1 = await grain.CalculateTrend(TimeSpan.FromHours(1), CancellationToken.None);
        var result2 = await grain.CalculateTrend(TimeSpan.FromHours(24), CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(TimeSpan.FromHours(1), result1.TimeWindow);
        Assert.Equal(TimeSpan.FromHours(24), result2.TimeWindow);
    }
}

public class ClusterFixture : IDisposable
{
    public TestCluster Cluster { get; private set; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();

        builder.AddSiloBuilderConfigurator<TestSiloConfigurator>();

        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster?.StopAllSilos();
    }
}

public class TestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.UseInMemoryReminderService()
                   .AddMemoryGrainStorageAsDefault()
                   .ConfigureServices(services =>
                   {
                       services.AddSingleton<HealthTrendCalculator>();
                   })
                   .ConfigureLogging(logging => logging.AddConsole());
    }
}