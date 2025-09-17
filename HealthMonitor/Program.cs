using HealthMonitor;
using HealthMonitor.Components;
using HealthMonitor.Extensions;
using HealthMonitor.Hub;
using HealthMonitor.Model;
using HealthMonitor.Services;
using HealthMonitor.Services.HealthCheckServices;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ServiceConfigurations>(builder.Configuration.GetRequiredSection("healthCheckConfiguration"));

builder.AddKeyedRedisClient("orleans-redis");
builder.Logging.AddFilter("Orleans.Runtime.Placement.Rebalancing", LogLevel.Trace);
builder.UseOrleans(siloBuilder =>
{
    siloBuilder
        .Configure<GrainCollectionOptions>(o => { o.CollectionQuantum = TimeSpan.FromSeconds(30); })
        .Configure<ResourceOptimizedPlacementOptions>(o => { o.LocalSiloPreferenceMargin = 0; })
        .Configure<ActivationRebalancerOptions>(o =>
        {
            o.RebalancerDueTime = TimeSpan.FromSeconds(30);
            o.SessionCyclePeriod = TimeSpan.FromSeconds(30);
        });
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IHealthCheckServiceFactory, HealthCheckServiceFactory>();
builder.Services.ConfigureHealthChecks(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices();

builder.Services.AddScoped<IServiceRegistry, ServiceRegistry>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddHostedService<GrainInitializerBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.MapHub<NotificationHub>("notification");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HealthMonitor.Client._Imports).Assembly);

app.MapHealthChecks("/health");

var healthCheckApi = app.MapGroup("/api/healthcheck");
healthCheckApi.MapGet("/services", ([FromServices]IServiceRegistry appService, CancellationToken token)
    => TypedResults.Ok(appService.GetAllServices(token)));


var analyticApi = app.MapGroup("/api/analytics");
analyticApi.MapGet("/health-trend", async ([FromServices]IAnalyticsService analyticsService, CancellationToken token, [FromQuery] int days = 7)
    =>
{
    var results = await analyticsService.GetSystemHealthTrend(TimeSpan.FromDays(days), token);
    return TypedResults.Ok(results);
});
app.Run();