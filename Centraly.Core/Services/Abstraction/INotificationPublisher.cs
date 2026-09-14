using Centraly.Api.Contracts.Notifications;

namespace Centraly.Api.Services.Abstraction;

// Host-specific: Api implements this over SignalR (IHubContext<NotificationHub>), Desktop
// implements it as an in-process publish (e.g. a messenger to the UI thread) since there's
// no separate client to push to. NotificationService must depend on this, never on
// IHubContext directly, so it stays usable outside a web host.
public interface INotificationPublisher
{
    Task PublishToUserAsync(string userId, NotificationResponse notification, CancellationToken ct = default);
}
