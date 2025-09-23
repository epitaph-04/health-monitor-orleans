var builder = WebApplication.CreateBuilder(args);

builder.AddKeyedRedisClient("orleans-redis");
builder.Host.UseOrleans(_ => { });
builder.Services.AddServicesForSelfHostedDashboard();
builder.Services.AddDashboard();

var app = builder.Build();
app.UseOrleansDashboard();

app.Run();
