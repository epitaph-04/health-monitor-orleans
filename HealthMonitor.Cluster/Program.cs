using HealthMonitor.Cluster;
using HealthMonitor.Cluster.Grains;
using HealthMonitor.Cluster.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.AddKeyedRedisClient("orleans-redis");
builder.UseOrleans(siloBuilder => siloBuilder
    .Configure<GrainCollectionOptions>(o => o.CollectionQuantum = TimeSpan.FromSeconds(30))
    .Configure<ResourceOptimizedPlacementOptions>(o => o.LocalSiloPreferenceMargin = 0)
    .Configure<ActivationRebalancerOptions>(o =>
    {
        o.RebalancerDueTime = TimeSpan.FromSeconds(5);
        o.SessionCyclePeriod = TimeSpan.FromSeconds(5);
    }));

builder.Services.AddHttpClient<HttpHealthCheckGrain>();
builder.Services.AddTransient<HealthTrendCalculator>();
builder.Services.AddHostedService<GrainInitializerService>();
var app = builder.Build();
await app.RunAsync();