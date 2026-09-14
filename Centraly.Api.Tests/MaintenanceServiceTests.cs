using Centraly.Api.Abstractions.Consts;
using Centraly.Api.Contracts.Drawer;
using Centraly.Api.Contracts.Maintenance;
using Centraly.Api.Contracts.Shared.Enums;
using Centraly.Api.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Centraly.Api.Tests;

// Covers report items #7 (maintenance cash movements bypassed FinancePolicy entirely -
// they always hit the drawer directly, never TransactionRouterService) and #10
// (a fresh, unpriced ticket showed a negative RemainingAmount as soon as a deposit
// was taken).
public class MaintenanceServiceTests
{
    [Fact]
    public async Task Deposit_WithNoConfiguredPolicy_StillDefaultsToDrawer_LikeBeforeTheFix()
    {
        using var db = new TestDb();
        await ServiceFactory.Drawer(db.Context).OpenSessionAsync(new OpenSessionRequest(0m, 2), "user-1"); // Maintenance drawer

        var service = ServiceFactory.Maintenance(db.Context);
        var result = await service.CreateMaintenanceAsync(
            new CreateMaintenanceRequest("عميل تجريبي", null, null, null, null, PaidAmount: 200m, DeliveryDate: null),
            "user-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await db.Context.DrawerTransactions.CountAsync());
        Assert.Equal(0, await db.Context.SafeTransactions.CountAsync());
    }

    [Fact]
    public async Task Deposit_WithSafeOnlyPolicyConfigured_RoutesToSafe_NotDrawer()
    {
        using var db = new TestDb();
        // No drawer session opened at all - if maintenance still bypassed the router and
        // hit the drawer directly (the old bug), this would fail with NoActiveSession
        // instead of succeeding via the safe.
        db.Context.TransactionSourcePolicies.Add(new TransactionSourcePolicy
        {
            Category = GlobalTransactionCategory.MaintenanceIncome,
            AllowedSource = PaymentSourcePolicy.SafeOnly
        });
        await db.Context.SaveChangesAsync();

        var service = ServiceFactory.Maintenance(db.Context);
        var result = await service.CreateMaintenanceAsync(
            new CreateMaintenanceRequest("عميل تجريبي", null, null, null, null, PaidAmount: 200m, DeliveryDate: null),
            "user-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await db.Context.DrawerTransactions.CountAsync());
        Assert.Equal(1, await db.Context.SafeTransactions.CountAsync());

        var mainSafe = await db.Context.Safes.AsNoTracking().FirstAsync(s => s.Id == DefaultSafe.MainSafeId);
        Assert.Equal(200m, mainSafe.Balance);
    }

    [Fact]
    public async Task Deposit_BeforePricing_RemainingAmountIsClampedToZero_NotNegative()
    {
        using var db = new TestDb();
        await ServiceFactory.Drawer(db.Context).OpenSessionAsync(new OpenSessionRequest(0m, 2), "user-1");

        var service = ServiceFactory.Maintenance(db.Context);
        var result = await service.CreateMaintenanceAsync(
            new CreateMaintenanceRequest("عميل تجريبي", null, null, null, null, PaidAmount: 200m, DeliveryDate: null),
            "user-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.TotalPrice);
        Assert.Equal(200m, result.Value.PaidAmount);
        // Before the fix this was -200 (0 - 200) until the ticket was later priced.
        Assert.Equal(0m, result.Value.RemainingAmount);
    }
}
