// OrderCompletedConsumer unit tests.
// Verifies the MassTransit consumer sends an OrderUpdate message to the specific buyer
// via SignalR IHubContext.Clients.User(buyerId) when an OrderCompletedEvent is consumed.

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

public class OrderCompletedConsumerTests
{
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<OrderCompletedConsumer>> _loggerMock = new();

    public OrderCompletedConsumerTests()
    {
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
    }

    [Fact]
    public async Task Consume_SendsOrderUpdateToSpecificBuyer()
    {
        var buyerId = "buyer-123";
        var orderId = Guid.NewGuid();
        var evt = new OrderCompletedEvent(Guid.NewGuid(), orderId, buyerId);

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);

        var consumer = new OrderCompletedConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderCompletedEvent>();

            _clientsMock.Verify(c => c.User(buyerId), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync("OrderUpdate",
                    It.Is<object[]>(args =>
                        args.Length == 1
                        && args[0] is OrderUpdateMessage
                        && ((OrderUpdateMessage)args[0]).Status == "Completed"
                        && ((OrderUpdateMessage)args[0]).OrderId == orderId
                        && ((OrderUpdateMessage)args[0]).BuyerId == buyerId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
