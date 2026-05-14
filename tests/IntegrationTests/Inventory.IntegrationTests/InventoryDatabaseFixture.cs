using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Inventory.IntegrationTests;

public class InventoryDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;

    public InventoryDatabaseFixture()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("inventory_db_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public string ConnectionString => _dbContainer.GetConnectionString();

    public InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new InventoryDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
