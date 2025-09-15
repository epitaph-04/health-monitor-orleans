using HealthMonitor.Model;
using Microsoft.AspNetCore.SignalR;

namespace HealthMonitor.Hub;

public class NotificationHub : Hub<INotificationClient>
{
    // public override async Task OnConnectedAsync()
    // {
    //     //await Clients.Client(Context.ConnectionId).ReceiveAllNotification(statusService.GetServices());
    //     await base.OnConnectedAsync();
    // }
}

public interface INotificationClient
{
    Task ReceiveNotification(string serviceId, HealthCheckResult checkResults);
}