using MembershipSystem.Adapters;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MembershipSystem.Adapters.Tests;

/// <summary>
/// A real SQLite database for adapter tests — an open in-memory
/// connection (not EF's InMemory provider), so adapters run against
/// actual SQL, actual constraints, and the actual EF Core SQLite
/// provider named in Stack &amp; conventions.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public MembershipDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MembershipDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new MembershipDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
