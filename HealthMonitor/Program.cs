using HealthMonitor;
using HealthMonitor.Components;
using HealthMonitor.Extensions;
using HealthMonitor.Hub;
using HealthMonitor.Model;
using HealthMonitor.Services;
using HealthMonitor.Services.HealthCheckServices;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using StackExchange.Redis;

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

builder.Services.AddScoped<ApplicationServices>();
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
healthCheckApi.MapGet("/services", (ApplicationServices appService, CancellationToken token)
    => TypedResults.Ok(appService.GetServices(token)));

app.Run();