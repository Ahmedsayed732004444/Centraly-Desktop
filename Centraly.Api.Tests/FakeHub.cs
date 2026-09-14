using Centraly.Api.Contracts.Notifications;
using Centraly.Api.Services.Abstraction;

namespace Centraly.Api.Tests;

// A minimal stand-in for the real SignalR-backed publisher so NotificationService can be
// tested as the real class (persistence + broadcast) without an actual Hub connection.
// Records every publish call so tests can assert who got notified and with what.
internal sealed class FakeNotificationPublisher : INotificationPublisher
{
    public Dictionary<string, List<NotificationResponse>> ByUser { get; } = [];

    public Task PublishToUserAsync(string userId, NotificationResponse notification, CancellationToken ct = default)
    {
        if (!ByUser.TryGetValue(userId, out var list))
        {
            list = [];
            ByUser[userId] = list;
        }
        list.Add(notification);
        return Task.CompletedTask;
    }
}

// No-op stand-in for IFileStorage - tests never exercise real file uploads (request.Image
// is always null in test requests), so this only needs to satisfy the constructor.
internal sealed class NoOpFileStorage : IFileStorage
{
    public Task<string?> SaveAsync(Microsoft.AspNetCore.Http.IFormFile? file, string folder, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public void Delete(string? storedPath, string folder) { }
}
