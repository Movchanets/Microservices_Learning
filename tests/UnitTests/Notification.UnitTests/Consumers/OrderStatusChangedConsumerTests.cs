// OrderStatusChangedConsumer unit tests.
// Verifies the MassTransit consumer sends an OrderUpdate message to the specific buyer
// via SignalR IHubContext.Clients.User(buyerId) when an OrderStatusChangedEvent is consumed.
// Covers: status propagation, notes handling (null/empty/present), correct routing.

using BuildingBlocks.SharedContracts.Events.Ordering;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Worker.Consumers;
using Notification.Worker.Hubs;
using Notification.Worker.Models;

namespace Notification.UnitTests.Consumers;

public class OrderStatusChangedConsumerTests
{
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<OrderStatusChangedConsumer>> _loggerMock = new();

    public OrderStatusChangedConsumerTests()
    {
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
    }

    [Fact]
    public async Task Consume_SendsStatusUpdateToSpecificBuyer()
    {
        var buyerId = "buyer-status-001";
        var orderId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var evt = new OrderStatusChangedEvent(orderId, buyerId, "PaymentProcessing", null, timestamp);

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);

        var consumer = new OrderStatusChangedConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderStatusChangedEvent>();

            _clientsMock.Verify(c => c.User(buyerId), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync("OrderUpdate",
                    It.Is<object[]>(args =>
                        args.Length == 1
                        && args[0] is OrderUpdateMessage
                        && ((OrderUpdateMessage)args[0]).Status == "PaymentProcessing"
                        && ((OrderUpdateMessage)args[0]).OrderId == orderId
                        && ((OrderUpdateMessage)args[0]).BuyerId == buyerId
                        && ((OrderUpdateMessage)args[0]).Reason == null
                        && ((OrderUpdateMessage)args[0]).Timestamp == timestamp),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_WithNotes_IncludesNotesInMessage()
    {
        var buyerId = "buyer-status-002";
        var orderId = Guid.NewGuid();
        var notes = "Inventory reserved successfully";
        var evt = new OrderStatusChangedEvent(orderId, buyerId, "InventoryReserved", notes, DateTime.UtcNow);

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);

        var consumer = new OrderStatusChangedConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderStatusChangedEvent>();

            _clientProxyMock.Verify(
                p => p.SendCoreAsync("OrderUpdate",
                    It.Is<object[]>(args =>
                        args.Length == 1
                        && args[0] is OrderUpdateMessage
                        && ((OrderUpdateMessage)args[0]).Status == "InventoryReserved"
                        && ((OrderUpdateMessage)args[0]).Reason == notes),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_WithNullNotes_SendsNullReason()
    {
        var buyerId = "buyer-status-003";
        var orderId = Guid.NewGuid();
        var evt = new OrderStatusChangedEvent(orderId, buyerId, "Submitted", null, DateTime.UtcNow);

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);

        var consumer = new OrderStatusChangedConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderStatusChangedEvent>();

            _clientProxyMock.Verify(
                p => p.SendCoreAsync("OrderUpdate",
                    It.Is<object[]>(args =>
                        args.Length == 1
                        && args[0] is OrderUpdateMessage
                        && ((OrderUpdateMessage)args[0]).Reason == null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_DoesNotSendToOtherBuyers()
    {
        var buyerId = "buyer-status-004";
        var otherBuyerId = "buyer-other";
        var orderId = Guid.NewGuid();
        var evt = new OrderStatusChangedEvent(orderId, buyerId, "Shipped", null, DateTime.UtcNow);

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.User(otherBuyerId)).Returns(new Mock<IClientProxy>().Object);

        var consumer = new OrderStatusChangedConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderStatusChangedEvent>();

            _clientsMock.Verify(c => c.User(buyerId), Times.Once);
            _clientsMock.Verify(c => c.User(otherBuyerId), Times.Never);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Theory]
    [InlineData("Submitted")]
    [InlineData("InventoryReserved")]
    [InlineData("PaymentProcessing")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    public async Task Consume_PassesStatusThroughUnchanged(string status)
    {
        var buyerId = "buyer-status-005";
        var orderId = Guid.NewGuid();
        var evt = new OrderStatusChangedEvent(orderId, buyerId, status, null, DateTime.UtcNow);

        OrderUpdateMessage? capturedMessage = null;
        _clientProxyMock
            .Setup(x => x.SendCoreAsync("OrderUpdate", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) =>
            {
                if (args.Length > 0 && args[0] is OrderUpdateMessage msg)
                    capturedMessage = msg;
            })
            .Returns(Task.CompletedTask);

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);

        var consumer = new OrderStatusChangedConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderStatusChangedEvent>();

            capturedMessage.Should().NotBeNull();
            capturedMessage!.Status.Should().Be(status);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
