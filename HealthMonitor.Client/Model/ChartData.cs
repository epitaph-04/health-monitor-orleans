namespace HealthMonitor.Client.Model;

public class ChartData
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
    public string Label { get; set; } = "";
    public string Color { get; set; } = "";
}