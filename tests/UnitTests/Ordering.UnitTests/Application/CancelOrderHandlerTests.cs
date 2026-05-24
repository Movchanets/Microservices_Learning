// CancelOrderHandler unit tests.
// Tests order cancellation via MediatR handler: successful cancellation publishes
// CancelOrderEvent to the saga, while cancelling a non-existent or terminal order returns failure.

using FluentAssertions;
using Moq;
using MassTransit;
using BuildingBlocks.SharedContracts.Events.Ordering;
using Ordering.Application.Commands.CancelOrder;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.UnitTests.Application;

public class CancelOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly CancelOrderHandler _handler;

    public CancelOrderHandlerTests()
    {
        _handler = new CancelOrderHandler(_repositoryMock.Object, _publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_PublishesCancelOrderEvent()
    {
        var order = Order.Create("buyer-1");
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new CancelOrderCommand(order.Id, "changed mind"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _publishEndpointMock.Verify(
            p => p.Publish(It.Is<CancelOrderEvent>(
                e => e.CorrelationId == order.Id
                  && e.OrderId == order.Id
                  && e.BuyerId == "buyer-1"
                  && e.Reason == "changed mind"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(
            new CancelOrderCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<CancelOrderEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ReturnsFailure()
    {
        var order = Order.Create("buyer-1");
        order.MarkInventoryReserved();
        order.MarkPaymentProcessing();
        order.MarkCompleted();

        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new CancelOrderCommand(order.Id, "too late"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<CancelOrderEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyCancelledOrder_ReturnsFailure()
    {
        var order = Order.Create("buyer-1");
        order.MarkCancelled("duplicate request");

        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new CancelOrderCommand(order.Id, "again"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<CancelOrderEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithFaultedOrder_ReturnsFailure()
    {
        var order = Order.Create("buyer-1");
        order.MarkFaulted("inventory reservation failed");

        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new CancelOrderCommand(order.Id, "clean up"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<CancelOrderEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
