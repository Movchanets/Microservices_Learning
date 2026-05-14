using FluentAssertions;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventory.IntegrationTests;

[Collection("Database collection")]
public class InventoryItemRepositoryTests
{
    private readonly InventoryDatabaseFixture _fixture;

    public InventoryItemRepositoryTests(InventoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_InventoryItem()
    {
        // Arrange
        var sku = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
        var item = InventoryItem.Create(sku, 100);

        // Act - Save
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var repository = new InventoryItemRepository(dbContext);
            repository.Add(item);
            await dbContext.SaveChangesAsync();
        }

        // Assert - Retrieve
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var repository = new InventoryItemRepository(dbContext);
            var retrievedItem = await repository.GetBySkuAsync(sku);

            retrievedItem.Should().NotBeNull();
            retrievedItem!.Sku.Should().Be(sku);
            retrievedItem.AvailableQuantity.Should().Be(100);
            retrievedItem.Version.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task Concurrent_Modification_Throws_DbUpdateConcurrencyException()
    {
        // Arrange
        var sku = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
        var item = InventoryItem.Create(sku, 50);

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var repository = new InventoryItemRepository(dbContext);
            repository.Add(item);
            await dbContext.SaveChangesAsync();
        }

        // Create two separate contexts simulating concurrent transactions
        await using var contextA = _fixture.CreateDbContext();
        await using var contextB = _fixture.CreateDbContext();

        var repoA = new InventoryItemRepository(contextA);
        var repoB = new InventoryItemRepository(contextB);

        var itemA = await repoA.GetBySkuAsync(sku);
        var itemB = await repoB.GetBySkuAsync(sku);

        itemA.Should().NotBeNull();
        itemB.Should().NotBeNull();

        // Act - Context A modifies and saves successfully
        itemA!.AddStock(10);
        repoA.Update(itemA);
        await contextA.SaveChangesAsync();

        // Act - Context B modifies the stale entity and tries to save
        itemB!.Reserve(5);
        repoB.Update(itemB);

        // Assert - Context B should throw a concurrency exception
        var act = async () => await contextB.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
