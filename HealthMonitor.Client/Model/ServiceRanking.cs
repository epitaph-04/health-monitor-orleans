using HealthMonitor.Model;

namespace HealthMonitor.Client.Model;

public class ServiceRanking
{
    public string ServiceId { get; set; } = "";
    public int Rank { get; set; }
    public double HealthScore { get; set; }
    public double Availability { get; set; }
    public HealthTrendDirection Trend { get; set; }
}