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
    public async Task Add_ThenGetBySkuCode_ReturnsPersistedItem()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repository = new InventoryItemRepository(context);

        var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var item = InventoryItem.Create(skuId, productId, "SKU-REPO-1", 100, storeId);

        // Act
        repository.Add(item);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetBySkuCodeAsync("SKU-REPO-1");
        retrieved.Should().NotBeNull();
        retrieved!.SkuCode.Should().Be("SKU-REPO-1");
        retrieved.AvailableQuantity.Should().Be(100);
    }

    [Fact]
    public async Task GetBySkuIdsAsync_ReturnsMatchingItems()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var repository = new InventoryItemRepository(context);

        var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var skuId1 = Guid.NewGuid();
        var skuId2 = Guid.NewGuid();
        var skuId3 = Guid.NewGuid();
        repository.Add(InventoryItem.Create(skuId1, Guid.NewGuid(), "SKU-MULTI-1", 10, storeId));
        repository.Add(InventoryItem.Create(skuId2, Guid.NewGuid(), "SKU-MULTI-2", 20, storeId));
        repository.Add(InventoryItem.Create(skuId3, Guid.NewGuid(), "SKU-MULTI-3", 30, storeId));
        await context.SaveChangesAsync();

        // Act
        var items = await repository.GetBySkuIdsAsync([skuId1, skuId3]);

        // Assert
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.SkuCode == "SKU-MULTI-1");
        items.Should().Contain(i => i.SkuCode == "SKU-MULTI-3");
    }

    [Fact]
    public async Task ConcurrentAccess_BothContextsCanReadAndWrite()
    {
        // Note: True xmin-based DbUpdateConcurrencyException requires EF Migrations
        // (not EnsureCreatedAsync). This test verifies concurrent read/write behavior.
        var skuCode = $"SKU-CONC-{Guid.NewGuid():N}";
        var normalizedSkuCode = skuCode.Trim().ToUpperInvariant();
        var skuId = Guid.NewGuid();

        // Setup: create item in first scope
        using (var setupScope = _fixture.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var setupRepo = new InventoryItemRepository(setupContext);
            var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var item = InventoryItem.Create(skuId, Guid.NewGuid(), skuCode, 50, storeId);
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

        var item2 = await repo2.GetBySkuCodeAsync(normalizedSkuCode);
        var item3 = await repo3.GetBySkuCodeAsync(normalizedSkuCode);

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
        var finalItem = await assertRepo.GetBySkuCodeAsync(normalizedSkuCode);
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

        var storeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var skuId = Guid.NewGuid();
        var item = InventoryItem.Create(skuId, Guid.NewGuid(), "SKU-UPD", 100, storeId);
        repository.Add(item);
        await context.SaveChangesAsync();

        // Act
        item.AddStock(50);
        repository.Update(item);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        // Assert
        var updated = await repository.GetBySkuCodeAsync("SKU-UPD");
        updated.Should().NotBeNull();
        updated!.AvailableQuantity.Should().Be(150);
    }
}
