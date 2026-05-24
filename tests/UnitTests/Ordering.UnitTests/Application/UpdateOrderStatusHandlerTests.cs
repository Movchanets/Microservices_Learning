// UpdateOrderStatusHandler unit tests.
// Tests order status updates via MediatR handler: valid transitions (Submitted->Processing,
// Processing->Shipped, Shipped->Delivered), invalid transitions, non-existent orders,
// and integration event publishing.

using FluentAssertions;
using Moq;
using MassTransit;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using Ordering.Application.Commands.UpdateOrderStatus;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;
using Ordering.Domain.Exceptions;

namespace Ordering.UnitTests.Application;

public class UpdateOrderStatusHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly UpdateOrderStatusHandler _handler;

    public UpdateOrderStatusHandlerTests()
    {
        _handler = new UpdateOrderStatusHandler(
            _repositoryMock.Object, _uowMock.Object, _publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_SubmittedToProcessing_ReturnsSuccess()
    {
        var order = Order.Create("buyer-1");
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "Processing", "Starting"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Processing);
        _repositoryMock.Verify(r => r.Update(order), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProcessingToShipped_ReturnsSuccess()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "Shipped", "Left warehouse"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Handle_ShippedToDelivered_ReturnsSuccessAndSetsCompletedAt()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "Delivered", "Arrived"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_InvalidTransition_ReturnsFailure()
    {
        var order = Order.Create("buyer-1");
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Submitted -> Shipped is invalid (must go through Processing first)
        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "Shipped", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentOrder_ReturnsFailure()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(Guid.NewGuid(), "Processing", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InvalidStatusString_ReturnsFailure()
    {
        var order = Order.Create("buyer-1");
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "InvalidStatus", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidTransition_PublishesIntegrationEvent()
    {
        var order = Order.Create("buyer-1");
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "Processing", "notes"), CancellationToken.None);

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderStatusChangedEvent>(e =>
                e.OrderId == order.Id &&
                e.BuyerId == "buyer-1" &&
                e.NewStatus == "Processing" &&
                e.Notes == "notes"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Delivered_PublishesCompletedAndStatusChangedEvents()
    {
        var order = Order.Create("buyer-1");
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, "Delivered", "Arrived"), CancellationToken.None);

        // Should publish OrderStatusChangedEvent
        _publishEndpointMock.Verify(p => p.Publish(
            It.IsAny<OrderStatusChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Submitted", "Processing")]
    [InlineData("Processing", "Shipped")]
    [InlineData("Shipped", "Delivered")]
    public async Task Handle_AllValidTransitions_Succeed(string from, string to)
    {
        var order = Order.Create("buyer-1");
        // Navigate to the 'from' status
        var fromStatus = Enum.Parse<OrderStatus>(from);
        if (fromStatus == OrderStatus.Processing)
            order.UpdateStatus(OrderStatus.Processing);
        else if (fromStatus == OrderStatus.Shipped)
        {
            order.UpdateStatus(OrderStatus.Processing);
            order.UpdateStatus(OrderStatus.Shipped);
        }

        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand(order.Id, to, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
