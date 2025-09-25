using HealthMonitor.Cluster;
using HealthMonitor.Cluster.Grains;
using HealthMonitor.Cluster.Services;
using HealthMonitor.Grains.Abstraction;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddKeyedRedisClient("orleans-redis");
builder.UseOrleans(siloBuilder => siloBuilder
    .Configure<GrainCollectionOptions>(o => o.CollectionQuantum = TimeSpan.FromSeconds(30))
    .Configure<ResourceOptimizedPlacementOptions>(o => o.LocalSiloPreferenceMargin = 0)
    .Configure<ActivationRebalancerOptions>(o =>
    {
        o.RebalancerDueTime = TimeSpan.FromSeconds(5);
        o.SessionCyclePeriod = TimeSpan.FromSeconds(5);
    })
    //.UseDashboard()
);
//builder.Services.AddServicesForSelfHostedDashboard();
//builder.Services.AddDashboard();

builder.Services.Configure<HealthTrendsOptions>(builder.Configuration.GetSection("HealthTrends"));
builder.Services.AddHttpClient<HttpHealthCheckGrain>();
builder.Services.AddTransient<HealthTrendCalculator>();
builder.Services.AddHostedService<GrainInitializerService>();

var app = builder.Build();
//app.UseOrleansDashboard();
app.MapHealthChecks("/health");
app.Run();