using BuildingBlocks.SharedContracts.Events.Catalog;
using FluentAssertions;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Repositories;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.IntegrationTests;

[Collection("Database collection")]
public class ProductCreatedConsumerTests
{
    private readonly InventoryDatabaseFixture _fixture;

    public ProductCreatedConsumerTests(InventoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProductCreatedEvent_CreatesNewInventoryItemWithZeroStock()
    {
        // Arrange
        var sku = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        await using var serviceProvider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<ProductCreatedConsumer>();
            })
            .AddScoped<InventoryDbContext>(sp => _fixture.CreateDbContext())
            .AddScoped<IInventoryItemRepository>(sp => new InventoryItemRepository(sp.GetRequiredService<InventoryDbContext>()))
            .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>())
            .AddSingleton(NullLogger<ProductCreatedConsumer>.Instance)
            .BuildServiceProvider(true);

        var harness = serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new ProductCreatedEvent(
            productId,
            "Test Product",
            "Description",
            99.99m,
            sku,
            "image.png",
            categoryId,
            "CategoryName",
            new List<string>(),
            null,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        // Act
        await harness.Bus.Publish(message);

        // Assert
        (await harness.Consumed.Any<ProductCreatedEvent>()).Should().BeTrue();
        (await harness.GetConsumerHarness<ProductCreatedConsumer>().Consumed.Any<ProductCreatedEvent>()).Should().BeTrue();

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var repository = new InventoryItemRepository(dbContext);
            var item = await repository.GetBySkuAsync(sku);

            item.Should().NotBeNull();
            item!.Sku.Should().Be(sku);
            item.AvailableQuantity.Should().Be(0);
        }
    }
}
