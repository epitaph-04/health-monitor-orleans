using MudBlazor;

namespace HealthMonitor.Client.Model;

public class ServiceFilter
{
    public string ServiceId { get; set; } = "";
    public bool IsSelected { get; set; }
    public Color StatusColor { get; set; } = Color.Default;
}