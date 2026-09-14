using Centraly.Api.Abstractions.Consts;
using Centraly.Api.Entities.Notifications;
using Centraly.Api.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Centraly.Api.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task NotifyUserAsync_PersistsRow_AndBroadcastsToThatUser()
    {
        using var db = new TestDb();
        var publisher = new FakeNotificationPublisher();
        var service = new NotificationService(db.Context, publisher);

        await service.NotifyUserAsync("user-1", NotificationType.OwnerWithdrawal, NotificationSeverity.Warning, "عنوان", "نص الإشعار");

        var stored = await db.Context.Notifications.AsNoTracking().SingleAsync();
        Assert.Equal("user-1", stored.UserId);
        Assert.False(stored.IsRead);

        Assert.True(publisher.ByUser.ContainsKey("user-1"));
        Assert.Single(publisher.ByUser["user-1"]);
    }

    [Fact]
    public async Task NotifyRolesAsync_TargetsOnlyUsersInThoseRoles()
    {
        // UserRoleConfiguration/UserConfiguration seed exactly one real user per
        // DefaultRole (Admin/Manager/Salesperson/Technician) via HasData, which
        // EnsureCreated applies - no extra seeding needed here.
        using var db = new TestDb();
        var publisher = new FakeNotificationPublisher();
        var service = new NotificationService(db.Context, publisher);

        await service.NotifyRolesAsync(["Admin", "Manager"], NotificationType.ProductLowStock, NotificationSeverity.Warning, "عنوان", "نص");

        var rows = await db.Context.Notifications.AsNoTracking().ToListAsync();
        var recipientIds = rows.Select(r => r.UserId).ToHashSet();

        Assert.Equal(2, rows.Count);
        Assert.Contains(DefaultUsers.Admin.Id, recipientIds);
        Assert.Contains(DefaultUsers.Manager.Id, recipientIds);
        Assert.DoesNotContain(DefaultUsers.Salesperson.Id, recipientIds);
        Assert.DoesNotContain(DefaultUsers.Technician.Id, recipientIds);

        Assert.True(publisher.ByUser.ContainsKey(DefaultUsers.Admin.Id));
        Assert.True(publisher.ByUser.ContainsKey(DefaultUsers.Manager.Id));
        Assert.False(publisher.ByUser.ContainsKey(DefaultUsers.Salesperson.Id));
    }

    [Fact]
    public async Task MarkAsRead_ThenUnreadCount_ReflectsIt()
    {
        using var db = new TestDb();
        var service = ServiceFactory.Notification(db.Context);

        await service.NotifyUserAsync("user-1", NotificationType.OwnerWithdrawal, NotificationSeverity.Info, "t", "b");
        var notification = await db.Context.Notifications.AsNoTracking().SingleAsync();

        var beforeCount = await service.GetUnreadCountAsync("user-1");
        Assert.Equal(1, beforeCount.Value.Count);

        var markResult = await service.MarkAsReadAsync("user-1", notification.Id);
        Assert.True(markResult.IsSuccess);

        var afterCount = await service.GetUnreadCountAsync("user-1");
        Assert.Equal(0, afterCount.Value.Count);
    }

    [Fact]
    public async Task MarkAsRead_ForAnotherUsersNotification_Fails()
    {
        using var db = new TestDb();
        var service = ServiceFactory.Notification(db.Context);

        await service.NotifyUserAsync("owner-user", NotificationType.OwnerWithdrawal, NotificationSeverity.Info, "t", "b");
        var notification = await db.Context.Notifications.AsNoTracking().SingleAsync();

        var result = await service.MarkAsReadAsync("someone-else", notification.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.NotFound", result.Error.Code);
    }
}
