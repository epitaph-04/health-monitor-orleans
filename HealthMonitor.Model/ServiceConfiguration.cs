using Orleans;

namespace HealthMonitor.Model;

public class ServiceConfigurations : List<ServiceConfiguration>;

[GenerateSerializer]
public class ServiceConfiguration
{
    [Id(0)]
    public string Id { get; set; } = null!;
    [Id(1)]
    public string Name { get; set; } = null!;
    [Id(2)]
    public ServiceType Type { get; set; }
    [Id(3)]
    public string Target { get; set; } = null!;
    [Id(4)]
    public int ExpectedResponseCode { get; set; } = 200;
    [Id(5)]
    public string Method { get; set; } = "GET";
    [Id(6)]
    public string? RequestBody { get; set; }
    [Id(7)]
    public string? Query { get; set; }
    [Id(8)]
    public int TimeoutSeconds { get; set; } = 30;
    [Id(9)]
    public int IntervalMinutes { get; set; } = 1;
    [Id(10)]
    public Dictionary<string, string>? Headers { get; set; }
}
