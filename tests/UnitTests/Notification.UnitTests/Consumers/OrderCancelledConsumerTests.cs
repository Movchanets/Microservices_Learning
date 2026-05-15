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

public class OrderCancelledConsumerTests
{
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<OrderCancelledConsumer>> _loggerMock = new();

    public OrderCancelledConsumerTests()
    {
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
    }

    [Fact]
    public async Task Consume_SendsCancelledUpdateWithReason()
    {
        var buyerId = "buyer-456";
        var orderId = Guid.NewGuid();
        var evt = new OrderCancelledEvent(Guid.NewGuid(), orderId, buyerId, "payment declined");

        _clientsMock.Setup(c => c.User(buyerId)).Returns(_clientProxyMock.Object);

        var consumer = new OrderCancelledConsumer(_hubContextMock.Object, _loggerMock.Object);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => consumer);

        await harness.Start();
        try
        {
            await harness.InputQueueSendEndpoint.Send(evt);

            await consumerHarness.Consumed.Any<OrderCancelledEvent>();

            _clientsMock.Verify(c => c.User(buyerId), Times.Once);
            _clientProxyMock.Verify(
                p => p.SendCoreAsync("OrderUpdate",
                    It.Is<object[]>(args =>
                        args.Length == 1
                        && args[0] is OrderUpdateMessage
                        && ((OrderUpdateMessage)args[0]).Status == "Cancelled"
                        && ((OrderUpdateMessage)args[0]).Reason == "payment declined"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
