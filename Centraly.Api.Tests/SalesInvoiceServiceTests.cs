using Centraly.Api.Contracts.Drawer;
using Centraly.Api.Contracts.Sales;
using Centraly.Api.Contracts.Shared.Enums;
using Centraly.Api.Entities.Inventory;
using Xunit;

namespace Centraly.Api.Tests;

// Covers report item #6: a retail invoice item priced at the batch's (lower) wholesale
// price used to pass validation as long as it matched *either* price field, regardless
// of the invoice's own SaleType.
public class SalesInvoiceServiceTests
{
    private static async Task<ProductBatch> SeedProductWithBatchAsync(TestDb db, int quantity = 10, decimal retail = 120m, decimal wholesale = 100m)
    {
        var department = new Department { Name = "قسم تجريبي" };
        var category = new Category { Name = "تصنيف تجريبي", DepartmentId = department.Id, Department = department };
        var product = new Product
        {
            Name = "منتج تجريبي",
            Barcode = Guid.NewGuid().ToString("N"),
            DepartmentId = department.Id,
            Department = department,
            CategoryId = category.Id,
            Category = category,
            Quantity = quantity
        };
        var batch = new ProductBatch
        {
            ProductId = product.Id,
            Product = product,
            InitialQuantity = quantity,
            AvailableQuantity = quantity,
            PurchasePrice = 70m,
            WholesalePrice = wholesale,
            RetailPrice = retail,
            DateReceived = DateTime.UtcNow
        };

        db.Context.Departments.Add(department);
        db.Context.Categories.Add(category);
        db.Context.Products.Add(product);
        db.Context.ProductBatches.Add(batch);
        await db.Context.SaveChangesAsync();

        return batch;
    }

    [Fact]
    public async Task RetailSale_PricedAtWholesalePrice_IsRejected()
    {
        using var db = new TestDb();
        var batch = await SeedProductWithBatchAsync(db);

        var service = ServiceFactory.SalesInvoice(db.Context);
        var request = new CreateSalesInvoiceRequest(
            CustomerId: null, CustomerName: null, CustomerPhone: null,
            SaleType: SaleTypeDto.Retail, PaymentMethod: PaymentMethodDto.Cash,
            PaidAmount: 100m, Notes: null,
            Items: [new CreateSalesInvoiceItemRequest(batch.ProductId, batch.Id, 1, 100m)]); // wholesale price on a retail sale

        var result = await service.AddInvoiceAsync(request, "user-1");

        Assert.True(result.IsFailure);
        Assert.Equal("SalesInvoice.InvalidPrice", result.Error.Code);
    }

    [Fact]
    public async Task RetailSale_PricedAtRetailPrice_Succeeds()
    {
        using var db = new TestDb();
        var batch = await SeedProductWithBatchAsync(db);
        await ServiceFactory.Drawer(db.Context).OpenSessionAsync(new OpenSessionRequest(0m, 1), "user-1");

        var service = ServiceFactory.SalesInvoice(db.Context);
        var request = new CreateSalesInvoiceRequest(
            CustomerId: null, CustomerName: null, CustomerPhone: null,
            SaleType: SaleTypeDto.Retail, PaymentMethod: PaymentMethodDto.Cash,
            PaidAmount: 120m, Notes: null,
            Items: [new CreateSalesInvoiceItemRequest(batch.ProductId, batch.Id, 1, 120m)],
            PaymentSource: PaymentSource.Drawer);

        var result = await service.AddInvoiceAsync(request, "user-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(120m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task WholesaleSale_PricedAtRetailPrice_IsRejected()
    {
        using var db = new TestDb();
        var batch = await SeedProductWithBatchAsync(db);

        var service = ServiceFactory.SalesInvoice(db.Context);
        var request = new CreateSalesInvoiceRequest(
            CustomerId: null, CustomerName: null, CustomerPhone: null,
            SaleType: SaleTypeDto.Wholesale, PaymentMethod: PaymentMethodDto.Cash,
            PaidAmount: 120m, Notes: null,
            Items: [new CreateSalesInvoiceItemRequest(batch.ProductId, batch.Id, 1, 120m)]); // retail price on a wholesale sale

        var result = await service.AddInvoiceAsync(request, "user-1");

        Assert.True(result.IsFailure);
        Assert.Equal("SalesInvoice.InvalidPrice", result.Error.Code);
    }
}
