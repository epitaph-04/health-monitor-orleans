namespace HealthMonitor.Client.Model;

public class ResponseTimeDataPoint
{
    public DateTime Time { get; set; }
    public double ResponseTime { get; set; }
    public string Service { get; set; } = "";
}