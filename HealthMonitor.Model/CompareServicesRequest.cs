namespace HealthMonitor.Model;

public class CompareServicesRequest
{
    public List<string> ServiceIds { get; set; } = new();
    public int Hours { get; set; } = 24;
}