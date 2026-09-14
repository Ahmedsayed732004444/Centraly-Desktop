using Centraly.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Centraly.Api.Tests;

// SQLite (used only as a fast in-memory substitute for SQL Server in tests) has no
// native rowversion/timestamp auto-generation, so [Timestamp] concurrency-token columns
// (DrawerSession.Version, Product.Version, ProductBatch.Version) would fail their
// NOT NULL constraint on insert. Give them a non-null default here so business-logic
// tests can run; this does not exercise true concurrency races (SQLite won't bump these
// on UPDATE the way SQL Server does) - that guarantee is provided by
// DrawerService.RecordTransactionAsync's retry loop and reviewed by hand.
public class SqliteTestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.IsConcurrencyToken && property.ClrType == typeof(byte[]))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasDefaultValueSql("randomblob(8)");
                }
            }
        }
    }
}
