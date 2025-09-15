namespace HealthMonitor.Model;

public record ServiceMetadata
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public ServiceType ServiceType { get; init; } = ServiceType.Http;
    public List<Service> DependentServices { get; init; } = new();
}