using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Inventory;
using FluentAssertions;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Repositories;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.IntegrationTests.Consumers;

[Collection("Inventory collection")]
public class ReservationConsumerTests
{
    private readonly InventoryDatabaseFixture _fixture;

    public ReservationConsumerTests(InventoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // ── ReserveInventoryConsumer ──────────────────────────────────────────

    [Fact]
    public async Task ReserveInventory_SufficientStock_PublishesReservedEvent_AndReducesStock()
    {
        // Arrange — seed an item with 10 units
        var sku = $"SKU-RESRV-{Guid.NewGuid():N}".ToUpperInvariant();
        using (var seedScope = _fixture.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var repo = new InventoryItemRepository(ctx);
            repo.Add(InventoryItem.Create(sku, 10));
            await ctx.SaveChangesAsync();
        }

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract> { new(sku, 3, 25.00m) };
        var command = new ReserveInventoryCommand(correlationId, orderId, items);

        // Resolve real ISender from DI (MediatR with real handlers)
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        InventoryReservedEvent? captured = null;
        var consumeContext = new Mock<ConsumeContext<ReserveInventoryCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<InventoryReservedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReservedEvent, CancellationToken>((evt, _) => captured = evt);

        var consumer = new ReserveInventoryConsumer(
            sender, Mock.Of<ILogger<ReserveInventoryConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert — event published with correct ids
        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(correlationId);
        captured.OrderId.Should().Be(orderId);

        // Assert — stock reduced in database
        using var assertScope = _fixture.CreateScope();
        var assertCtx = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var assertRepo = new InventoryItemRepository(assertCtx);
        var updated = await assertRepo.GetBySkuAsync(sku);
        updated.Should().NotBeNull();
        updated!.AvailableQuantity.Should().Be(7); // 10 - 3
    }

    [Fact]
    public async Task ReserveInventory_InsufficientStock_PublishesFailureEvent()
    {
        // Arrange — seed an item with only 1 unit
        var sku = $"SKU-LOW-{Guid.NewGuid():N}".ToUpperInvariant();
        using (var seedScope = _fixture.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var repo = new InventoryItemRepository(ctx);
            repo.Add(InventoryItem.Create(sku, 1));
            await ctx.SaveChangesAsync();
        }

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract> { new(sku, 5, 10.00m) };
        var command = new ReserveInventoryCommand(correlationId, orderId, items);

        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        InventoryReservationFailedEvent? captured = null;
        var consumeContext = new Mock<ConsumeContext<ReserveInventoryCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<InventoryReservationFailedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReservationFailedEvent, CancellationToken>((evt, _) => captured = evt);

        var consumer = new ReserveInventoryConsumer(
            sender, Mock.Of<ILogger<ReserveInventoryConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert — failure event published
        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(correlationId);
        captured.OrderId.Should().Be(orderId);
        captured.Reason.Should().NotBeNullOrWhiteSpace();

        // Assert — stock unchanged
        using var assertScope = _fixture.CreateScope();
        var assertCtx = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var assertRepo = new InventoryItemRepository(assertCtx);
        var unchanged = await assertRepo.GetBySkuAsync(sku);
        unchanged.Should().NotBeNull();
        unchanged!.AvailableQuantity.Should().Be(1);
    }

    [Fact]
    public async Task ReserveInventory_MultipleItems_ReservesAllOrFails()
    {
        // Arrange — two items, both in stock
        var sku1 = $"SKU-M1-{Guid.NewGuid():N}".ToUpperInvariant();
        var sku2 = $"SKU-M2-{Guid.NewGuid():N}".ToUpperInvariant();
        using (var seedScope = _fixture.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var repo = new InventoryItemRepository(ctx);
            repo.Add(InventoryItem.Create(sku1, 10));
            repo.Add(InventoryItem.Create(sku2, 20));
            await ctx.SaveChangesAsync();
        }

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract>
        {
            new(sku1, 4, 15.00m),
            new(sku2, 7, 30.00m)
        };
        var command = new ReserveInventoryCommand(correlationId, orderId, items);

        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        InventoryReservedEvent? captured = null;
        var consumeContext = new Mock<ConsumeContext<ReserveInventoryCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<InventoryReservedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReservedEvent, CancellationToken>((evt, _) => captured = evt);

        var consumer = new ReserveInventoryConsumer(
            sender, Mock.Of<ILogger<ReserveInventoryConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert
        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(correlationId);

        using var assertScope = _fixture.CreateScope();
        var assertCtx = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var assertRepo = new InventoryItemRepository(assertCtx);

        var item1 = await assertRepo.GetBySkuAsync(sku1);
        item1!.AvailableQuantity.Should().Be(6); // 10 - 4

        var item2 = await assertRepo.GetBySkuAsync(sku2);
        item2!.AvailableQuantity.Should().Be(13); // 20 - 7
    }

    // ── CancelReservationConsumer ─────────────────────────────────────────

    [Fact]
    public async Task CancelReservation_PublishesReleasedEvent_AndRestoresStock()
    {
        // Arrange — seed item with 10 stock, then reserve 4 (so 6 remaining)
        var sku = $"SKU-CANC-{Guid.NewGuid():N}".ToUpperInvariant();
        using (var seedScope = _fixture.CreateScope())
        {
            var ctx = seedScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var repo = new InventoryItemRepository(ctx);
            var item = InventoryItem.Create(sku, 10);
            item.Reserve(4); // 10 → 6
            repo.Add(item);
            await ctx.SaveChangesAsync();
        }

        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract> { new(sku, 4, 25.00m) };
        var command = new CancelReservationCommand(correlationId, orderId, items);

        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        InventoryReleasedEvent? captured = null;
        var consumeContext = new Mock<ConsumeContext<CancelReservationCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<InventoryReleasedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReleasedEvent, CancellationToken>((evt, _) => captured = evt);

        var consumer = new CancelReservationConsumer(
            sender, Mock.Of<ILogger<CancelReservationConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert — event published
        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(correlationId);
        captured.OrderId.Should().Be(orderId);

        // Assert — stock restored
        using var assertScope = _fixture.CreateScope();
        var assertCtx = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var assertRepo = new InventoryItemRepository(assertCtx);
        var updated = await assertRepo.GetBySkuAsync(sku);
        updated.Should().NotBeNull();
        updated!.AvailableQuantity.Should().Be(10); // 6 + 4
    }
}
