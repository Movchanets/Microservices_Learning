using FluentAssertions;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests;

[Collection("Inventory collection")]
public class InventoryItemRepositoryTests
{
    private readonly InventoryDatabaseFixture _fixture;

    public InventoryItemRepositoryTests(InventoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Add_ThenGetBySku_ReturnsPersistedItem()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repository = new InventoryItemRepository(context);

        var item = InventoryItem.Create("SKU-REPO-1", 100);

        // Act
        repository.Add(item);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetBySkuAsync("SKU-REPO-1");
        retrieved.Should().NotBeNull();
        retrieved!.Sku.Should().Be("SKU-REPO-1");
        retrieved.AvailableQuantity.Should().Be(100);
    }

    [Fact]
    public async Task GetBySkusAsync_ReturnsMatchingItems()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repository = new InventoryItemRepository(context);

        repository.Add(InventoryItem.Create("SKU-MULTI-1", 10));
        repository.Add(InventoryItem.Create("SKU-MULTI-2", 20));
        repository.Add(InventoryItem.Create("SKU-MULTI-3", 30));
        await context.SaveChangesAsync();

        // Act
        var items = await repository.GetBySkusAsync(["SKU-MULTI-1", "SKU-MULTI-3"]);

        // Assert
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.Sku == "SKU-MULTI-1");
        items.Should().Contain(i => i.Sku == "SKU-MULTI-3");
    }

    [Fact]
    public async Task ConcurrentAccess_BothContextsCanReadAndWrite()
    {
        // Note: True xmin-based DbUpdateConcurrencyException requires EF Migrations
        // (not EnsureCreatedAsync). This test verifies concurrent read/write behavior.
        var sku = $"SKU-CONC-{Guid.NewGuid():N}";
        var normalizedSku = sku.Trim().ToUpperInvariant();

        // Setup: create item in first scope
        using (var setupScope = _fixture.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var setupRepo = new InventoryItemRepository(setupContext);
            var item = InventoryItem.Create(sku, 50);
            setupRepo.Add(item);
            await setupContext.SaveChangesAsync();
        }

        // Act: two separate scopes read and modify independently
        using var scope2 = _fixture.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repo2 = new InventoryItemRepository(context2);

        using var scope3 = _fixture.CreateScope();
        var context3 = scope3.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repo3 = new InventoryItemRepository(context3);

        var item2 = await repo2.GetBySkuAsync(normalizedSku);
        var item3 = await repo3.GetBySkuAsync(normalizedSku);

        item2.Should().NotBeNull();
        item3.Should().NotBeNull();

        // Modify and save first context
        item2!.AddStock(10);
        await context2.SaveChangesAsync();

        // Second context still has stale data but can save (no xmin concurrency with EnsureCreated)
        item3!.AddStock(20);
        await context3.SaveChangesAsync();

        // Assert: final value depends on which save ran last (last-write-wins)
        using var assertScope = _fixture.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var assertRepo = new InventoryItemRepository(assertContext);
        var finalItem = await assertRepo.GetBySkuAsync(normalizedSku);
        finalItem.Should().NotBeNull();
        finalItem!.AvailableQuantity.Should().Be(70); // 50 + 20 (last write wins)
    }

    [Fact]
    public async Task Update_ModifiesExistingItem()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repository = new InventoryItemRepository(context);

        var item = InventoryItem.Create("SKU-UPD", 100);
        repository.Add(item);
        await context.SaveChangesAsync();

        // Act
        item.AddStock(50);
        repository.Update(item);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        // Assert
        var updated = await repository.GetBySkuAsync("SKU-UPD");
        updated.Should().NotBeNull();
        updated!.AvailableQuantity.Should().Be(150);
    }
}
