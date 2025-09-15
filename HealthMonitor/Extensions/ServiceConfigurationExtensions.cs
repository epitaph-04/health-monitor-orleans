using HealthMonitor.Model;
using HealthMonitor.Services.HealthCheckServices;

namespace HealthMonitor.Extensions;

public static class ServiceConfigurationExtensions
{
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        ServiceConfigurations serviceConfigurations = new ServiceConfigurations();
        configuration.GetSection("healthCheckConfiguration").Bind(serviceConfigurations);
        foreach (var serviceConfiguration in serviceConfigurations)
        {
            services.AddKeyedSingleton<IHealthCheckService>(serviceConfiguration.Id, (provider, _) =>
            {
                return serviceConfiguration.Type switch
                {
                    ServiceType.Http => new HttpHealthCheckService(
                        provider.GetRequiredService<HttpClient>(), serviceConfiguration, provider.GetRequiredService<ILogger<HttpHealthCheckService>>()),
                    _ => throw new ArgumentOutOfRangeException()
                };
            });
        }
        return services;
    }
}