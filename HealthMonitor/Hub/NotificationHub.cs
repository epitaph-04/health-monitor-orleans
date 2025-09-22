using HealthMonitor.Model;
using Microsoft.AspNetCore.SignalR;

namespace HealthMonitor.Hub;

public class NotificationHub : Hub<INotificationClient> { }

public interface INotificationClient
{
    Task ReceiveNotification(string serviceId, HealthCheckRecord checkResults);
}