using Centraly.Api.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centraly.Api.Tests;

// A fresh in-memory SQLite database per test, with a connection kept open for the
// lifetime of the DbContext (required for ":memory:" to survive across the multiple
// SaveChanges/transactions each service call makes) and schema created directly from
// the current model (no need to replay historical SQL-Server-only migrations).
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public ApplicationDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new SqliteTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
