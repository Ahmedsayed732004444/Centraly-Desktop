using Centraly.Api.Abstractions.Consts;
using Centraly.Api.Contracts.Inventory.Products;
using Centraly.Api.Entities.Inventory;
using Centraly.Api.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Centraly.Api.Tests;

// Covers report item #12: AdjustQuantityAsync let stock go negative with no lower
// bound, and DeleteProductAsync soft-deleted a product without checking it still had
// stock on hand, silently disappearing it (and its value) from inventory reports.
// Also covers the stock-alert notification wiring (notifications plan, phase 2).
public class ProductServiceTests
{
    private static async Task<Product> SeedProductAsync(TestDb db, int quantity, int minQuantityAlert = 0)
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
            Quantity = quantity,
            MinQuantityAlert = minQuantityAlert
        };

        db.Context.Departments.Add(department);
        db.Context.Categories.Add(category);
        db.Context.Products.Add(product);
        await db.Context.SaveChangesAsync();

        return product;
    }

    [Fact]
    public async Task AdjustQuantity_ToNegative_IsRejected()
    {
        using var db = new TestDb();
        var product = await SeedProductAsync(db, quantity: 10);

        var service = ServiceFactory.Product(db.Context);
        var result = await service.AdjustQuantityAsync(product.Id, new AdjustProductQuantityRequest("batch-1", -5, "جرد"), "user-1");

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NegativeQuantity", result.Error.Code);
    }

    [Fact]
    public async Task Delete_ProductWithStockRemaining_IsRejected()
    {
        using var db = new TestDb();
        var product = await SeedProductAsync(db, quantity: 5);

        var service = ServiceFactory.Product(db.Context);
        var result = await service.DeleteProductAsync(product.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.CannotDeleteWithStock", result.Error.Code);
    }

    [Fact]
    public async Task Delete_ProductWithZeroStock_Succeeds()
    {
        using var db = new TestDb();
        var product = await SeedProductAsync(db, quantity: 0);

        var service = ServiceFactory.Product(db.Context);
        var result = await service.DeleteProductAsync(product.Id);

        Assert.True(result.IsSuccess);
    }

    // UserRoleConfiguration/UserConfiguration seed one real user per DefaultRole via
    // HasData (applied by EnsureCreated) - NotifyStockChangeAsync targets Admin+Manager,
    // so every crossing notifies exactly those 2 pre-seeded users, no extra seeding needed.
    [Fact]
    public async Task AdjustQuantity_CrossingIntoLowStock_NotifiesAdminAndManager()
    {
        using var db = new TestDb();
        var product = await SeedProductAsync(db, quantity: 20, minQuantityAlert: 5);

        var service = ServiceFactory.Product(db.Context);
        var result = await service.AdjustQuantityAsync(product.Id, new AdjustProductQuantityRequest("batch-1", 3, "جرد"), "user-1");

        Assert.True(result.IsSuccess);

        var notifications = await db.Context.Notifications.AsNoTracking().ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n => Assert.Equal(NotificationType.ProductLowStock, n.Type));
        Assert.Contains(notifications, n => n.UserId == DefaultUsers.Admin.Id);
        Assert.Contains(notifications, n => n.UserId == DefaultUsers.Manager.Id);
    }

    [Fact]
    public async Task AdjustQuantity_StayingLowStock_DoesNotReNotify()
    {
        using var db = new TestDb();
        // Already at/below MinQuantityAlert before this adjustment - not a fresh crossing.
        var product = await SeedProductAsync(db, quantity: 4, minQuantityAlert: 5);

        var service = ServiceFactory.Product(db.Context);
        var result = await service.AdjustQuantityAsync(product.Id, new AdjustProductQuantityRequest("batch-1", 3, "جرد"), "user-1");

        Assert.True(result.IsSuccess);
        Assert.Empty(await db.Context.Notifications.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AdjustQuantity_CrossingIntoOutOfStock_NotifiesAsCritical()
    {
        using var db = new TestDb();
        var product = await SeedProductAsync(db, quantity: 3, minQuantityAlert: 5);

        var service = ServiceFactory.Product(db.Context);
        var result = await service.AdjustQuantityAsync(product.Id, new AdjustProductQuantityRequest("batch-1", 0, "بيع آخر قطعة"), "user-1");

        Assert.True(result.IsSuccess);

        var notifications = await db.Context.Notifications.AsNoTracking().ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n =>
        {
            Assert.Equal(NotificationType.ProductOutOfStock, n.Type);
            Assert.Equal(NotificationSeverity.Critical, n.Severity);
        });
    }
}
