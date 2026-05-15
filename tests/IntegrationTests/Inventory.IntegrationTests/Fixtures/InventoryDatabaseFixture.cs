using Marketplace.IntegrationTests.Shared;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Inventory.IntegrationTests;

public sealed class InventoryDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = PostgresContainerFactory.Create("inventory_tests");

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var services = new ServiceCollection();

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Inventory.Application.Commands.ReserveStockCommand).Assembly));

        services.AddScoped<Inventory.Domain.Aggregates.IInventoryItemRepository,
            Inventory.Infrastructure.Repositories.InventoryItemRepository>();

        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();

        // Create schema
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await context.Database.EnsureCreatedAsync();

        // EF Core + EnsureCreated creates Version as a regular bytea NOT NULL column
        // but IsRowVersion() maps to xmin (system column). Set a default so inserts work.
        await context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"InventoryItems\" ALTER COLUMN \"Version\" SET DEFAULT '\\x00000001';");
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public IServiceScope CreateScope() => ServiceProvider.CreateScope();
}

[CollectionDefinition("Inventory collection")]
public class InventoryCollection : ICollectionFixture<InventoryDatabaseFixture>
{
}
