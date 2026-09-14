using Centraly.Api.Abstractions;
using Centraly.Api.Persistence;
using Centraly.Api.Services.Abstraction;
using Centraly.Api.Services.Implementation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Centraly.Api.Tests;

// Builds the real service implementations wired to a shared ApplicationDbContext, the
// same way DI would, but by hand - there's no test project convention to follow yet.
// Only production classes are used (no mocks) so these are effectively small
// integration tests against SQLite standing in for SQL Server.
internal static class ServiceFactory
{
    public static DrawerService Drawer(ApplicationDbContext db) => new(db);

    public static SafeService Safe(ApplicationDbContext db) => new(db);

    // A fresh HybridCache per call (never shared/static) - its default backing is an
    // in-process cache keyed by plain strings like "financePolicies-MaintenanceIncome",
    // so reusing one instance across tests would leak cached values from one test's
    // TestDb into another's assertions.
    public static HybridCache NewHybridCache() =>
        new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>();

    public static FinancePolicyService FinancePolicy(ApplicationDbContext db) => new(db, NewHybridCache());

    public static TransactionRouterService Router(ApplicationDbContext db) =>
        new(FinancePolicy(db), Drawer(db), Safe(db));

    public static CustomerTransactionService CustomerTransaction(ApplicationDbContext db) =>
        new(db, NullLogger<CustomerTransactionService>.Instance, Router(db), new NotImplementedReturnProcessingService());

    // Real NotificationService backed by a FakeNotificationPublisher (see FakeHub.cs) -
    // exercises actual persistence + broadcast-call recording without a live SignalR connection.
    public static NotificationService Notification(ApplicationDbContext db) => new(db, new FakeNotificationPublisher());

    public static SalesInvoiceService SalesInvoice(ApplicationDbContext db) =>
        new(db, NullLogger<SalesInvoiceService>.Instance, Router(db), Notification(db));

    public static MaintenanceService Maintenance(ApplicationDbContext db) =>
        new(db, Router(db), NullLogger<MaintenanceService>.Instance, Notification(db));

    public static ProductService Product(ApplicationDbContext db) =>
        new(db, new NoOpFileStorage(), NullLogger<ProductService>.Instance, Notification(db));

    // Not exercised by any test below (AddPaymentAsync only calls it for the
    // "with items to return" flow) - a real one needs its own dependency chain.
    private sealed class NotImplementedReturnProcessingService : IReturnProcessingService
    {
        public Task<Result<Centraly.Api.Contracts.Returns.ReturnRecordResponse>> ProcessReturnAsync(
            Centraly.Api.Contracts.Returns.CreateCustomerReturnRequest request, string? userId, string? expectedCustomerId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}
