using HealthMonitor.Hub;
using HealthMonitor.Model;
using Microsoft.AspNetCore.SignalR;

namespace HealthMonitor.Grains;

public interface INotifierGrains : IGrainWithIntegerKey
{
    public ValueTask Notify(string serviceId, HealthCheckRecord checkResults);
}

public class NotifierGrain(IHubContext<NotificationHub, INotificationClient> context) : Grain, INotifierGrains
{
    public async ValueTask Notify(string serviceId, HealthCheckRecord checkResults) 
        => await context.Clients.All.ReceiveNotification(serviceId, checkResults).ConfigureAwait(false);
}