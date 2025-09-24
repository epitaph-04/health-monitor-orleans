var builder = WebApplication.CreateBuilder(args);

builder.AddKeyedRedisClient("orleans-redis");
builder.Host.UseOrleans(_ => { });
builder.Services.AddServicesForSelfHostedDashboard(cfg => cfg.BasePath = "/orleans-dashboard");
builder.Services.AddDashboard(cfg => cfg.BasePath = "/orleans-dashboard");

var app = builder.Build();
app.Map("/orleans-dashboard", x => x.UseOrleansDashboard());

app.Run();
