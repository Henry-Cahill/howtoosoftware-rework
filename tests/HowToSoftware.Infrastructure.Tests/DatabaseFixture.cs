using HowToSoftware.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace HowToSoftware.Infrastructure.Tests;

/// <summary>
/// Shared xUnit fixture that starts a SQL Server container once per test collection.
/// Each test class gets its own DbContext (and can create scoped contexts) to avoid cross-test interference.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Apply EF Core migrations to create the schema
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString, o => o.EnableRetryOnFailure())
            .Options;

        return new AppDbContext(options);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
