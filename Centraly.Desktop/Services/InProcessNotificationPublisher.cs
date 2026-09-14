using Centraly.Api.Contracts.Notifications;

namespace Centraly.Desktop.Services;

public record NotificationReceivedMessage(NotificationResponse Notification);

// Desktop's INotificationPublisher: there's no separate client to push to over a wire -
// the signed-in user IS the process. Broadcast in-process; NotificationBell subscribes via
// WeakReferenceMessenger and marshals to the UI thread itself.
public class InProcessNotificationPublisher : INotificationPublisher
{
    public Task PublishToUserAsync(string userId, NotificationResponse notification, CancellationToken ct = default)
    {
        WeakReferenceMessenger.Default.Send(new NotificationReceivedMessage(notification));
        return Task.CompletedTask;
    }
}
