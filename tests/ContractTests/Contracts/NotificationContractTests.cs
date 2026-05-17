using BuildingBlocks.SharedContracts.Events.Ordering;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Worker.Consumers;
using Notification.Worker.Models;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Ordering events are correctly consumed
/// by the Notification microservice and produce the expected SignalR messages.
///
/// Tests the message contract between Ordering saga and Notification consumers,
/// ensuring OrderUpdateMessage has the correct shape for frontend consumption.
/// </summary>
public class NotificationContractTests
{
    [Fact]
    public async Task OrderCompletedEvent_Contract_ShouldSendCompletedUpdateViaSignalR()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var buyerId = "buyer-notify-001";
        var @event = new OrderCompletedEvent(Guid.NewGuid(), orderId, buyerId);

        OrderUpdateMessage? capturedMessage = null;
        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(x => x.SendCoreAsync("OrderUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                if (args.Length > 0 && args[0] is OrderUpdateMessage msg)
                    capturedMessage = msg;
            })
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(x => x.User(buyerId)).Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<Notification.Worker.Hubs.NotificationHub>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);

        var consumeContext = new Mock<ConsumeContext<OrderCompletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderCompletedConsumer(
            hubContextMock.Object, Mock.Of<ILogger<OrderCompletedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - verify the OrderUpdateMessage contract
        capturedMessage.Should().NotBeNull();
        capturedMessage!.OrderId.Should().Be(orderId);
        capturedMessage.BuyerId.Should().Be(buyerId);
        capturedMessage.Status.Should().Be("Completed");
        capturedMessage.Reason.Should().BeNull();
    }

    [Fact]
    public async Task OrderCancelledEvent_Contract_ShouldSendCancelledUpdateWithReason()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var buyerId = "buyer-notify-002";
        var reason = "Payment failed: Card declined";
        var @event = new OrderCancelledEvent(Guid.NewGuid(), orderId, buyerId, reason);

        OrderUpdateMessage? capturedMessage = null;
        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(x => x.SendCoreAsync("OrderUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                if (args.Length > 0 && args[0] is OrderUpdateMessage msg)
                    capturedMessage = msg;
            })
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(x => x.User(buyerId)).Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<Notification.Worker.Hubs.NotificationHub>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);

        var consumeContext = new Mock<ConsumeContext<OrderCancelledEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderCancelledConsumer(
            hubContextMock.Object, Mock.Of<ILogger<OrderCancelledConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.OrderId.Should().Be(orderId);
        capturedMessage.BuyerId.Should().Be(buyerId);
        capturedMessage.Status.Should().Be("Cancelled");
        capturedMessage.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task OrderStatusChangedEvent_Contract_ShouldSendStatusUpdate()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var buyerId = "buyer-notify-003";
        var timestamp = DateTime.UtcNow;
        var @event = new OrderStatusChangedEvent(
            orderId, buyerId, "Shipped", "Package dispatched via FedEx", timestamp);

        OrderUpdateMessage? capturedMessage = null;
        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(x => x.SendCoreAsync("OrderUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                if (args.Length > 0 && args[0] is OrderUpdateMessage msg)
                    capturedMessage = msg;
            })
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(x => x.User(buyerId)).Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<Notification.Worker.Hubs.NotificationHub>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);

        var consumeContext = new Mock<ConsumeContext<OrderStatusChangedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderStatusChangedConsumer(
            hubContextMock.Object, Mock.Of<ILogger<OrderStatusChangedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.OrderId.Should().Be(orderId);
        capturedMessage.BuyerId.Should().Be(buyerId);
        capturedMessage.Status.Should().Be("Shipped");
        capturedMessage.Reason.Should().Be("Package dispatched via FedEx");
        capturedMessage.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public async Task OrderCompletedEvent_Contract_ShouldRouteToCorrectUser()
    {
        // Arrange - verify SignalR routes to the correct buyer
        var buyerId = "buyer-routing-test";
        var @event = new OrderCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), buyerId);

        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(x => x.SendCoreAsync("OrderUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(x => x.User(buyerId)).Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<Notification.Worker.Hubs.NotificationHub>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);

        var consumeContext = new Mock<ConsumeContext<OrderCompletedEvent>>();
        consumeContext.Setup(x => x.Message).Returns(@event);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new OrderCompletedConsumer(
            hubContextMock.Object, Mock.Of<ILogger<OrderCompletedConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - verify routing to correct user
        clientsMock.Verify(x => x.User(buyerId), Times.Once);
        clientProxyMock.Verify(
            x => x.SendCoreAsync("OrderUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OrderUpdateMessage_Contract_ShouldHaveCorrectShape()
    {
        // Arrange - verify the OrderUpdateMessage shape matches frontend expectations
        var orderId = Guid.NewGuid();
        var buyerId = "buyer-shape-test";
        var timestamp = DateTime.UtcNow;

        var message = new OrderUpdateMessage(orderId, buyerId, "Processing", "Hold for review", timestamp);

        // Assert - all fields accessible and correct types
        message.OrderId.Should().Be(orderId);
        message.BuyerId.Should().Be(buyerId);
        message.Status.Should().Be("Processing");
        message.Reason.Should().Be("Hold for review");
        message.Timestamp.Should().Be(timestamp);

        // Verify it's a record (immutable)
        var message2 = message with { Status = "Shipped" };
        message.Status.Should().Be("Processing"); // Original unchanged
        message2.Status.Should().Be("Shipped");
    }
}
