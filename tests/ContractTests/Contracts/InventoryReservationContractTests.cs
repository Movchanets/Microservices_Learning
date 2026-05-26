using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Inventory;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Infrastructure.Messaging.Consumers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Inventory commands (ReserveInventoryCommand,
/// CancelReservationCommand) are correctly consumed and produce the expected events.
///
/// These tests use mocked ISender (MediatR) to verify message contracts
/// without requiring a database.
/// </summary>
public class InventoryReservationContractTests
{
    [Fact]
    public async Task ReserveInventoryCommand_Contract_ShouldPublishInventoryReservedOnSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var items = new List<OrderItemContract>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product 1", 2, 29.99m, Guid.Parse("33333333-3333-3333-3333-333333333333"))
        };

        var command = new ReserveInventoryCommand(correlationId, orderId, items);

        // Mock MediatR sender to return success
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Capture published events
        InventoryReservedEvent? publishedEvent = null;
        var publishContext = new Mock<ConsumeContext<ReserveInventoryCommand>>();
        publishContext.Setup(x => x.Message).Returns(command);
        publishContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        publishContext
            .Setup(x => x.Publish(It.IsAny<InventoryReservedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReservedEvent, CancellationToken>((evt, _) => publishedEvent = evt);

        var consumer = new ReserveInventoryConsumer(
            senderMock.Object, Mock.Of<ILogger<ReserveInventoryConsumer>>());

        // Act
        await consumer.Consume(publishContext.Object);

        // Assert - verify the published event contract
        publishedEvent.Should().NotBeNull();
        publishedEvent!.CorrelationId.Should().Be(correlationId);
        publishedEvent.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task ReserveInventoryCommand_Contract_ShouldPublishFailureEventOnError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var items = new List<OrderItemContract>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-5", "Product 5", 5, 10m, Guid.Parse("33333333-3333-3333-3333-333333333333"))
        };

        var command = new ReserveInventoryCommand(correlationId, orderId, items);

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Insufficient stock"));

        InventoryReservationFailedEvent? publishedEvent = null;
        var publishContext = new Mock<ConsumeContext<ReserveInventoryCommand>>();
        publishContext.Setup(x => x.Message).Returns(command);
        publishContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        publishContext
            .Setup(x => x.Publish(It.IsAny<InventoryReservationFailedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReservationFailedEvent, CancellationToken>((evt, _) => publishedEvent = evt);

        var consumer = new ReserveInventoryConsumer(
            senderMock.Object, Mock.Of<ILogger<ReserveInventoryConsumer>>());

        // Act
        await consumer.Consume(publishContext.Object);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.CorrelationId.Should().Be(correlationId);
        publishedEvent.OrderId.Should().Be(orderId);
        publishedEvent.Reason.Should().Be("Insufficient stock");
    }

    [Fact]
    public async Task CancelReservationCommand_Contract_ShouldPublishInventoryReleasedOnSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var items = new List<OrderItemContract>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-2", "Product 2", 2, 10m, Guid.Parse("33333333-3333-3333-3333-333333333333"))
        };

        var command = new CancelReservationCommand(correlationId, orderId, items);

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ReleaseStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        InventoryReleasedEvent? publishedEvent = null;
        var publishContext = new Mock<ConsumeContext<CancelReservationCommand>>();
        publishContext.Setup(x => x.Message).Returns(command);
        publishContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        publishContext
            .Setup(x => x.Publish(It.IsAny<InventoryReleasedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryReleasedEvent, CancellationToken>((evt, _) => publishedEvent = evt);

        var consumer = new CancelReservationConsumer(
            senderMock.Object, Mock.Of<ILogger<CancelReservationConsumer>>());

        // Act
        await consumer.Consume(publishContext.Object);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.CorrelationId.Should().Be(correlationId);
        publishedEvent.OrderId.Should().Be(orderId);
    }
}
