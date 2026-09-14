using Centraly.Api.Contracts.Customers;
using Centraly.Api.Contracts.Drawer;
using Centraly.Api.Contracts.Shared.Enums;
using Centraly.Api.Entities.Customers;
using Centraly.Api.Entities.Inventory;
using Centraly.Api.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Centraly.Api.Tests;

// Covers report items #2 (deferred-sale profit lost on a general payment), #3
// (PaidAmount could exceed TotalAmount and trip CK_Invoice_PaidAmount), and #4
// (banker's rounding on profit shares).
public class CustomerTransactionServiceTests
{
    private static async Task OpenSalesDrawerAsync(TestDb db, decimal openingBalance = 0m)
    {
        await ServiceFactory.Drawer(db.Context).OpenSessionAsync(new OpenSessionRequest(openingBalance, 1), "user-1");
    }

    // InvoiceItem.ProductId/BatchId are real foreign keys - seed one throwaway
    // product+batch per test and reuse its ids across every invoice item, since these
    // tests only care about the invoice/payment math, not the catalog data.
    private static async Task<ProductBatch> SeedProductBatchAsync(TestDb db)
    {
        var department = new Department { Name = "قسم" };
        var category = new Category { Name = "تصنيف", DepartmentId = department.Id, Department = department };
        var product = new Product { Name = "منتج", DepartmentId = department.Id, Department = department, CategoryId = category.Id, Category = category };
        var batch = new ProductBatch { ProductId = product.Id, Product = product, DateReceived = DateTime.UtcNow };

        db.Context.AddRange(department, category, product, batch);
        await db.Context.SaveChangesAsync();
        return batch;
    }

    private static async Task<Invoice> SeedDeferredInvoiceAsync(
        TestDb db, string customerId, ProductBatch batch, decimal totalAmount, decimal unitPrice, decimal unitCost, int quantity, DateTime createdAt)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            CustomerId = customerId,
            SaleType = Centraly.Api.Entities.Common.SaleType.Retail,
            PaymentMethod = Centraly.Api.Entities.Common.PaymentMethod.Deferred,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            UserId = "user-1"
        };
        invoice.Items.Add(new InvoiceItem
        {
            ProductId = batch.ProductId,
            BatchId = batch.Id,
            Quantity = quantity,
            UnitPrice = unitPrice,
            UnitCost = unitCost
        });

        db.Context.Invoices.Add(invoice);
        await db.Context.SaveChangesAsync();

        // ApplyAuditAndSoftDelete stamps CreatedAt=UtcNow on every insert, overriding
        // whatever was set beforehand - bypass it with a raw update so FIFO ordering
        // between invoices seeded in the same test is deterministic.
        await db.Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Invoices SET CreatedAt = {createdAt} WHERE Id = {invoice.Id}");

        return invoice;
    }

    [Fact]
    public async Task GeneralPayment_WithoutInvoiceId_RecognizesProfitOnOutstandingInvoice()
    {
        using var db = new TestDb();
        await OpenSalesDrawerAsync(db);

        var customer = new Customer { Name = "Test Customer", DebtBalance = 1000m };
        db.Context.Customers.Add(customer);
        await db.Context.SaveChangesAsync();

        var batch = await SeedProductBatchAsync(db);
        // TotalAmount=1000, 10 units @ 100 sold at cost 60 -> potential profit 400.
        await SeedDeferredInvoiceAsync(db, customer.Id, batch, 1000m, 100m, 60m, 10, DateTime.UtcNow.AddDays(-1));

        var service = ServiceFactory.CustomerTransaction(db.Context);
        var result = await service.AddPaymentAsync(
            customer.Id,
            new CreateCustomerPaymentRequest(1000m, "دفعة عامة", InvoiceId: null, PaymentSource.Drawer),
            "user-1");

        Assert.True(result.IsSuccess);

        var invoice = await db.Context.Invoices.AsNoTracking().FirstAsync();
        Assert.Equal(1000m, invoice.PaidAmount);
        // Before the fix this stayed 0 because AddPaymentAsync only ever looked at
        // request.InvoiceId, which a general payment never sets.
        Assert.Equal(400m, invoice.RecordedProfit);

        var reloadedCustomer = await db.Context.Customers.AsNoTracking().FirstAsync();
        Assert.Equal(0m, reloadedCustomer.DebtBalance);
    }

    [Fact]
    public async Task Payment_ExceedingSingleInvoiceDebt_ClampsPaidAmount_InsteadOfFailing()
    {
        using var db = new TestDb();
        await OpenSalesDrawerAsync(db);

        var customer = new Customer { Name = "Test Customer", DebtBalance = 500m };
        db.Context.Customers.Add(customer);
        await db.Context.SaveChangesAsync();

        var batch = await SeedProductBatchAsync(db);
        var invoice = await SeedDeferredInvoiceAsync(db, customer.Id, batch, 500m, 100m, 50m, 5, DateTime.UtcNow);

        var service = ServiceFactory.CustomerTransaction(db.Context);
        // Overpaying by 200 used to attempt invoice.PaidAmount = 700 > TotalAmount(500),
        // which trips CK_Invoice_PaidAmount and fails the whole payment.
        var result = await service.AddPaymentAsync(
            customer.Id,
            new CreateCustomerPaymentRequest(700m, null, invoice.Id, PaymentSource.Drawer),
            "user-1");

        Assert.True(result.IsSuccess);

        var reloadedInvoice = await db.Context.Invoices.AsNoTracking().FirstAsync();
        Assert.Equal(500m, reloadedInvoice.PaidAmount);
        Assert.Equal(250m, reloadedInvoice.RecordedProfit); // full profit, invoice fully settled

        var reloadedCustomer = await db.Context.Customers.AsNoTracking().FirstAsync();
        Assert.Equal(-200m, reloadedCustomer.DebtBalance); // excess becomes credit
    }

    [Fact]
    public async Task GeneralPayment_SweepsOldestInvoiceFirst_AcrossMultipleInvoices()
    {
        using var db = new TestDb();
        await OpenSalesDrawerAsync(db);

        var customer = new Customer { Name = "Test Customer", DebtBalance = 800m };
        db.Context.Customers.Add(customer);
        await db.Context.SaveChangesAsync();

        var batch = await SeedProductBatchAsync(db);
        var older = await SeedDeferredInvoiceAsync(db, customer.Id, batch, 300m, 300m, 200m, 1, DateTime.UtcNow.AddDays(-2)); // profit 100
        var newer = await SeedDeferredInvoiceAsync(db, customer.Id, batch, 500m, 500m, 300m, 1, DateTime.UtcNow.AddDays(-1)); // profit 200

        var service = ServiceFactory.CustomerTransaction(db.Context);
        var result = await service.AddPaymentAsync(
            customer.Id,
            new CreateCustomerPaymentRequest(400m, null, InvoiceId: null, PaymentSource.Drawer),
            "user-1");

        Assert.True(result.IsSuccess);

        var reloadedOlder = await db.Context.Invoices.AsNoTracking().FirstAsync(i => i.Id == older.Id);
        var reloadedNewer = await db.Context.Invoices.AsNoTracking().FirstAsync(i => i.Id == newer.Id);

        Assert.Equal(300m, reloadedOlder.PaidAmount);
        Assert.Equal(100m, reloadedOlder.RecordedProfit);

        Assert.Equal(100m, reloadedNewer.PaidAmount);
        Assert.Equal(40m, reloadedNewer.RecordedProfit); // 200 * (100/500)
    }

    [Fact]
    public async Task Refund_NegativeAmount_DoesNotTouchInvoiceProfitOrPaidAmount()
    {
        using var db = new TestDb();
        await OpenSalesDrawerAsync(db, openingBalance: 500m); // must cover the refund payout

        var customer = new Customer { Name = "Test Customer", DebtBalance = -100m }; // credit balance
        db.Context.Customers.Add(customer);
        await db.Context.SaveChangesAsync();

        var batch = await SeedProductBatchAsync(db);
        var invoice = await SeedDeferredInvoiceAsync(db, customer.Id, batch, 300m, 300m, 200m, 1, DateTime.UtcNow);

        var service = ServiceFactory.CustomerTransaction(db.Context);
        var result = await service.AddPaymentAsync(
            customer.Id,
            new CreateCustomerPaymentRequest(-100m, "استرداد", InvoiceId: null, PaymentSource.Drawer),
            "user-1");

        Assert.True(result.IsSuccess);

        var reloadedInvoice = await db.Context.Invoices.AsNoTracking().FirstAsync(i => i.Id == invoice.Id);
        Assert.Equal(0m, reloadedInvoice.PaidAmount);
        Assert.Equal(0m, reloadedInvoice.RecordedProfit);
    }
}
