using Centraly.Api.Abstractions.Consts;
using Centraly.Api.Entities.Common;
using Centraly.Api.Entities.Drawer;
using Centraly.Api.Entities.Maintenance;
using Centraly.Api.Entities.Notifications;
using Centraly.Api.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Centraly.Api.Tests;

// Covers the Hangfire recurring jobs (notifications plan, phase 2): maintenance
// due-soon/overdue sweeps and drawer-left-open detection, plus their dedup guards
// (each ticket/session is notified at most once per type across repeated runs).
public class NotificationBackgroundJobsTests
{
    private static NotificationBackgroundJobs Jobs(TestDb db) =>
        new(db.Context, ServiceFactory.Notification(db.Context), NullLogger<NotificationBackgroundJobs>.Instance);

    private static MaintenanceDevice SeedPendingTicket(TestDb db, DateTime? deliveryDate)
    {
        var device = new MaintenanceDevice
        {
            CustomerName = "عميل تجريبي",
            Status = MaintenanceStatus.Pending,
            DeliveryDate = deliveryDate
        };
        db.Context.MaintenanceDevices.Add(device);
        return device;
    }

    [Fact]
    public async Task CheckMaintenanceTickets_PastDeliveryDate_NotifiesOverdue_ToMaintenanceRoles()
    {
        using var db = new TestDb();
        SeedPendingTicket(db, DateTime.UtcNow.AddHours(-3));
        await db.Context.SaveChangesAsync();

        await Jobs(db).CheckMaintenanceTicketsAsync();

        var notifications = await db.Context.Notifications.AsNoTracking().ToListAsync();
        Assert.Equal(3, notifications.Count); // Admin + Manager + Technician (one seeded user each)
        Assert.All(notifications, n => Assert.Equal(NotificationType.MaintenanceOverdue, n.Type));
        var recipientIds = notifications.Select(n => n.UserId).ToHashSet();
        Assert.Contains(DefaultUsers.Technician.Id, recipientIds);
        Assert.DoesNotContain(DefaultUsers.Salesperson.Id, recipientIds);
    }

    [Fact]
    public async Task CheckMaintenanceTickets_DueWithinTwoHours_NotifiesDueSoon()
    {
        using var db = new TestDb();
        SeedPendingTicket(db, DateTime.UtcNow.AddHours(1));
        await db.Context.SaveChangesAsync();

        await Jobs(db).CheckMaintenanceTicketsAsync();

        var notifications = await db.Context.Notifications.AsNoTracking().ToListAsync();
        Assert.NotEmpty(notifications);
        Assert.All(notifications, n => Assert.Equal(NotificationType.MaintenanceDueSoon, n.Type));
    }

    [Fact]
    public async Task CheckMaintenanceTickets_FarInTheFuture_DoesNotNotify()
    {
        using var db = new TestDb();
        SeedPendingTicket(db, DateTime.UtcNow.AddHours(5));
        await db.Context.SaveChangesAsync();

        await Jobs(db).CheckMaintenanceTicketsAsync();

        Assert.Empty(await db.Context.Notifications.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task CheckMaintenanceTickets_RunTwice_DoesNotDuplicateNotifications()
    {
        using var db = new TestDb();
        SeedPendingTicket(db, DateTime.UtcNow.AddHours(-1));
        await db.Context.SaveChangesAsync();

        var job = Jobs(db);
        await job.CheckMaintenanceTicketsAsync();
        var firstRunCount = await db.Context.Notifications.CountAsync();

        await job.CheckMaintenanceTicketsAsync();
        var secondRunCount = await db.Context.Notifications.CountAsync();

        Assert.Equal(firstRunCount, secondRunCount);
        Assert.True(firstRunCount > 0);
    }

    [Fact]
    public async Task CheckDrawerSessionsLeftOpen_OpenPastThreshold_NotifiesBackOffice()
    {
        using var db = new TestDb();
        var session = new DrawerSession
        {
            Type = DrawerType.Sales,
            OpeningBalance = 100m,
            RunningBalance = 100m,
            OpenedByUserId = "user-1",
            IsClosed = false
        };
        db.Context.DrawerSessions.Add(session);
        await db.Context.SaveChangesAsync();
        await db.Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE DrawerSessions SET OpenedAt = {DateTime.UtcNow.AddHours(-15)} WHERE Id = {session.Id}");

        await Jobs(db).CheckDrawerSessionsLeftOpenAsync();

        var notifications = await db.Context.Notifications.AsNoTracking().ToListAsync();
        Assert.Equal(2, notifications.Count); // Admin + Manager
        Assert.All(notifications, n => Assert.Equal(NotificationType.DrawerLeftOpenOvernight, n.Type));
    }

    [Fact]
    public async Task CheckDrawerSessionsLeftOpen_RecentlyOpened_DoesNotNotify()
    {
        using var db = new TestDb();
        db.Context.DrawerSessions.Add(new DrawerSession
        {
            Type = DrawerType.Sales,
            OpeningBalance = 100m,
            RunningBalance = 100m,
            OpenedByUserId = "user-1",
            IsClosed = false
        });
        await db.Context.SaveChangesAsync();

        await Jobs(db).CheckDrawerSessionsLeftOpenAsync();

        Assert.Empty(await db.Context.Notifications.AsNoTracking().ToListAsync());
    }
}
