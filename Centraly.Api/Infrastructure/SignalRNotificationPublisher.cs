using Centraly.Api.Contracts.Notifications;
using Centraly.Api.Hubs;
using Centraly.Api.Services.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace Centraly.Api.Infrastructure;

// Web-hosted implementation of INotificationPublisher: pushes over the existing
// NotificationHub, same behavior NotificationService had inline before.
public class SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext) : INotificationPublisher
{
    public Task PublishToUserAsync(string userId, NotificationResponse notification, CancellationToken ct = default) =>
        hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification, ct);
}
