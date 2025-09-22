using ApexCharts;
using HealthMonitor;
using HealthMonitor.Client.Service;
using HealthMonitor.Components;
using HealthMonitor.Extensions;
using HealthMonitor.Hub;
using HealthMonitor.Model;
using HealthMonitor.Services;
using HealthMonitor.Services.BgService;
using HealthMonitor.Services.HealthCheckServices;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
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
    siloBuilder.UseDashboard(cfg => cfg.HostSelf = true);
});

builder.Services.AddHttpClient<HttpHealthCheckService>();
builder.Services.AddSingleton<IHealthCheckServiceFactory, HealthCheckServiceFactory>();
builder.Services.ConfigureHealthChecks(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
builder.Services.AddApexCharts();

builder.Services.AddScoped<IServiceRegistry, ServiceRegistry>();
builder.Services.AddScoped<IHealthTrendService, HealthTrendService>();
builder.Services.AddTransient<HealthTrendCalculator>();
builder.Services.AddHostedService<GrainInitializerBackgroundService>();
builder.Services.AddHostedService<HealthTrendCalculationService>();
builder.Services.AddScoped<DashboardService>();

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

var healthTrendApi = app.MapGroup("/api/healthtrends");
healthTrendApi.MapGet("/services", ([FromServices]IServiceRegistry appService, CancellationToken token)
    => TypedResults.Ok(appService.GetAllServices(token)));
healthTrendApi.MapGet("/service/{serviceId}", 
    async (string serviceId, [FromServices]IHealthTrendService service, CancellationToken token, [FromQuery] int hours = 24)
    =>
    {
        var trend = await service.GetServiceTrend(serviceId, hours, token);
        return TypedResults.Ok(trend);
    });
healthTrendApi.MapGet("/service/{serviceId}/history", 
    async (string serviceId, [FromServices]IHealthTrendService service, CancellationToken token, [FromQuery] int count = 10)
    =>
{
    var trend = await service.GetServiceTrendHistory(serviceId, count, token);
    return TypedResults.Ok(trend);
});
healthTrendApi.MapGet("/system/overview", 
    async ([FromServices]IHealthTrendService service, CancellationToken token)
        =>
    {
        var trend = await service.GetSystemOverview(token);
        return TypedResults.Ok(trend);
    });
healthTrendApi.MapPost("/compare", 
    async ([FromServices]IHealthTrendService service, [FromBody]CompareServicesRequest request, CancellationToken token)
        =>
    {
        var trend = await service.CompareServices(request, token);
        return TypedResults.Ok(trend);
    });
healthTrendApi.MapGet("/refresh", 
    async ([FromServices]IHealthTrendService service, CancellationToken token)
        =>
    {
        await service.RefreshAllTrends(token);
        return TypedResults.Ok(new { message = "Trend refresh initiated" });
    });

app.Map("/orleans-dashboard", x => x.UseOrleansDashboard());
app.Run();