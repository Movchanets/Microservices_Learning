using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Commands.Payment;
using BuildingBlocks.SharedContracts.Events.Payment;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.Commands.ProcessPayment;
using Payment.Infrastructure.External;
using Payment.Infrastructure.Messaging;

namespace ContractTests.Contracts;

/// <summary>
/// Contract tests verifying that Payment commands are correctly consumed
/// and produce the expected events.
///
/// Tests the ProcessPaymentConsumer with mocked gateway and MediatR sender,
/// ensuring the message contracts between Ordering saga and Payment service
/// are stable and compatible.
/// </summary>
public class PaymentContractTests
{
    [Fact]
    public async Task ProcessPaymentCommand_Contract_ShouldPublishPaymentCompletedOnSuccess()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand(correlationId, orderId, 99.99m, "buyer-123");

        var gatewayMock = new Mock<IPaymentGateway>();
        gatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, 99.99m, "buyer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(true, "txn_success_123", null));

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ProcessPaymentInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        PaymentCompletedEvent? publishedEvent = null;
        var consumeContext = new Mock<ConsumeContext<ProcessPaymentCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<PaymentCompletedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentCompletedEvent, CancellationToken>((evt, _) => publishedEvent = evt);

        var consumer = new ProcessPaymentConsumer(
            senderMock.Object, gatewayMock.Object, Mock.Of<ILogger<ProcessPaymentConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - verify the published event contract
        publishedEvent.Should().NotBeNull();
        publishedEvent!.CorrelationId.Should().Be(correlationId);
        publishedEvent.OrderId.Should().Be(orderId);
        publishedEvent.TransactionId.Should().Be("txn_success_123");
    }

    [Fact]
    public async Task ProcessPaymentCommand_Contract_ShouldPublishPaymentFailedOnFailure()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand(correlationId, orderId, 50.00m, "buyer-456");

        var gatewayMock = new Mock<IPaymentGateway>();
        gatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, 50.00m, "buyer-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(false, null, "Card declined"));

        var senderMock = new Mock<ISender>();

        PaymentFailedEvent? publishedEvent = null;
        var consumeContext = new Mock<ConsumeContext<ProcessPaymentCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<PaymentFailedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentFailedEvent, CancellationToken>((evt, _) => publishedEvent = evt);

        var consumer = new ProcessPaymentConsumer(
            senderMock.Object, gatewayMock.Object, Mock.Of<ILogger<ProcessPaymentConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.CorrelationId.Should().Be(correlationId);
        publishedEvent.OrderId.Should().Be(orderId);
        publishedEvent.FailureReason.Should().Be("Card declined");

        senderMock.Verify(
            x => x.Send(It.IsAny<ProcessPaymentInternalCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentCommand_Contract_ShouldPassCorrectAmountToGateway()
    {
        // Arrange - verify the amount contract is preserved
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand(correlationId, orderId, 1234.56m, "buyer-789");

        var gatewayMock = new Mock<IPaymentGateway>();
        gatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, 1234.56m, "buyer-789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(true, "txn_big", null));

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ProcessPaymentInternalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        PaymentCompletedEvent? publishedEvent = null;
        var consumeContext = new Mock<ConsumeContext<ProcessPaymentCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext
            .Setup(x => x.Publish(It.IsAny<PaymentCompletedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentCompletedEvent, CancellationToken>((evt, _) => publishedEvent = evt);

        var consumer = new ProcessPaymentConsumer(
            senderMock.Object, gatewayMock.Object, Mock.Of<ILogger<ProcessPaymentConsumer>>());

        // Act
        await consumer.Consume(consumeContext.Object);

        // Assert - gateway received exact amount
        gatewayMock.Verify(
            x => x.ProcessPaymentAsync(orderId, 1234.56m, "buyer-789", It.IsAny<CancellationToken>()),
            Times.Once);

        publishedEvent.Should().NotBeNull();
        publishedEvent!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task ProcessPaymentCommand_Contract_ShouldHandleGatewayException()
    {
        // Arrange - gateway throws (network error, timeout, etc.)
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand(correlationId, orderId, 10m, "buyer-error");

        var gatewayMock = new Mock<IPaymentGateway>();
        gatewayMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gateway timeout"));

        var senderMock = new Mock<ISender>();

        var consumeContext = new Mock<ConsumeContext<ProcessPaymentCommand>>();
        consumeContext.Setup(x => x.Message).Returns(command);
        consumeContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var consumer = new ProcessPaymentConsumer(
            senderMock.Object, gatewayMock.Object, Mock.Of<ILogger<ProcessPaymentConsumer>>());

        // Act & Assert - should propagate exception (MassTransit will handle retry)
        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(consumeContext.Object));
    }
}
