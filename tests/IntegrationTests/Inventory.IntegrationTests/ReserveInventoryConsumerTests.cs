using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Inventory;
using FluentAssertions;
using Inventory.Application.Commands;
using Inventory.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Inventory.IntegrationTests;

public class ReserveInventoryConsumerTests
{
    [Fact]
    public async Task ReserveInventoryCommand_Success_PublishesInventoryReservedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract> { new OrderItemContract("SKU1", 2) };

        var senderMock = new Mock<ISender>();
        senderMock.Setup(s => s.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        await using var serviceProvider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<ReserveInventoryConsumer>();
            })
            .AddSingleton(senderMock.Object)
            .AddSingleton(NullLogger<ReserveInventoryConsumer>.Instance)
            .BuildServiceProvider(true);

        var harness = serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new ReserveInventoryCommand(correlationId, orderId, items);

        // Act
        await harness.Bus.Publish(message);

        // Assert
        var consumerHarness = harness.GetConsumerHarness<ReserveInventoryConsumer>();
        (await harness.Consumed.Any<ReserveInventoryCommand>()).Should().BeTrue();
        (await consumerHarness.Consumed.Any<ReserveInventoryCommand>()).Should().BeTrue();

        (await harness.Published.Any<InventoryReservedEvent>()).Should().BeTrue();
    }

    [Fact]
    public async Task ReserveInventoryCommand_Failure_PublishesInventoryReservationFailedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var items = new List<OrderItemContract> { new OrderItemContract("SKU1", 2) };
        var errorMessage = "Out of stock";

        var senderMock = new Mock<ISender>();
        senderMock.Setup(s => s.Send(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(errorMessage));

        await using var serviceProvider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<ReserveInventoryConsumer>();
            })
            .AddSingleton(senderMock.Object)
            .AddSingleton(NullLogger<ReserveInventoryConsumer>.Instance)
            .BuildServiceProvider(true);

        var harness = serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new ReserveInventoryCommand(correlationId, orderId, items);

        // Act
        await harness.Bus.Publish(message);

        // Assert
        var consumerHarness = harness.GetConsumerHarness<ReserveInventoryConsumer>();
        (await harness.Consumed.Any<ReserveInventoryCommand>()).Should().BeTrue();
        (await consumerHarness.Consumed.Any<ReserveInventoryCommand>()).Should().BeTrue();

        (await harness.Published.Any<InventoryReservationFailedEvent>()).Should().BeTrue();
    }
}
