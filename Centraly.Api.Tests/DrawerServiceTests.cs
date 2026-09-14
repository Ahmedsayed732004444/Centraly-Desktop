using Centraly.Api.Contracts.Drawer;
using Centraly.Api.Entities.Common;
using Xunit;

namespace Centraly.Api.Tests;

// Covers report item #1: RunningBalance used to start at 0 instead of OpeningBalance,
// so a legitimate expense against a freshly opened drawer was wrongly rejected as
// InsufficientFunds, and every transaction's recorded Balance was short by the opening
// amount.
public class DrawerServiceTests
{
    [Fact]
    public async Task OpenSession_SeedsRunningBalance_SoAnImmediateExpenseSucceeds()
    {
        using var db = new TestDb();
        var drawer = ServiceFactory.Drawer(db.Context);

        var openResult = await drawer.OpenSessionAsync(new OpenSessionRequest(1000m, 1), "user-1");
        Assert.True(openResult.IsSuccess);

        var expenseResult = await drawer.RecordTransactionAsync(
            DrawerTransactionCategory.Expense, DrawerTransactionType.Expense,
            400m, -400m, "test expense", null, "user-1");

        Assert.True(expenseResult.IsSuccess);
        Assert.Equal(600m, expenseResult.Value.Balance);
    }

    [Fact]
    public async Task CloseSession_ClosingBalance_MatchesLastRecordedTransactionBalance()
    {
        using var db = new TestDb();
        var drawer = ServiceFactory.Drawer(db.Context);

        await drawer.OpenSessionAsync(new OpenSessionRequest(1000m, 1), "user-1");
        var incomeResult = await drawer.RecordTransactionAsync(
            DrawerTransactionCategory.Sales, DrawerTransactionType.Income, 250m, 100m, null, null, "user-1");

        var closeResult = await drawer.CloseSessionAsync(1, "user-1");

        Assert.True(closeResult.IsSuccess);
        // Before the fix these disagreed by exactly the opening balance (1000).
        Assert.Equal(incomeResult.Value.Balance, closeResult.Value.ClosingBalance);
        Assert.Equal(1250m, closeResult.Value.ClosingBalance);
    }

    [Fact]
    public async Task RecordTransaction_WithoutActiveSession_Fails()
    {
        using var db = new TestDb();
        var drawer = ServiceFactory.Drawer(db.Context);

        var result = await drawer.RecordTransactionAsync(
            DrawerTransactionCategory.Sales, DrawerTransactionType.Income, 100m, 0m, null, null, "user-1");

        Assert.True(result.IsFailure);
        Assert.Equal("Drawer.NoActiveSession", result.Error.Code);
    }
}
