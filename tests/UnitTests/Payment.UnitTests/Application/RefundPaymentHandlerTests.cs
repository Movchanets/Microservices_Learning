// RefundPaymentHandler unit tests.
// Verifies the handler creates a Refund, marks the original transaction as refunded,
// and prevents double-refunding.

using FluentAssertions;
using Moq;
using BuildingBlocks.SharedContracts.Abstractions;
using Payment.Application.Commands.RefundPayment;
using Payment.Domain.Aggregates;
using Payment.Domain.Enumerations;

namespace Payment.UnitTests.Application;

public class RefundPaymentHandlerTests
{
    private readonly Mock<IPaymentTransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IRefundRepository> _refundRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RefundPaymentHandler _handler;

    public RefundPaymentHandlerTests()
    {
        _handler = new RefundPaymentHandler(
            _transactionRepoMock.Object,
            _refundRepoMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task Handle_WithCompletedTransaction_CreatesRefundAndMarksTransactionRefunded()
    {
        var transaction = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 100m);
        transaction.MarkCompleted("txn_123");

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _refundRepoMock
            .Setup(r => r.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        var command = new RefundPaymentCommand(transaction.Id, "Customer request");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        transaction.Status.Should().Be(PaymentStatus.Refunded);
        _refundRepoMock.Verify(r => r.Add(It.IsAny<Refund>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentTransaction_ReturnsFailure()
    {
        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var command = new RefundPaymentCommand(Guid.NewGuid(), "reason");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Transaction not found");
    }

    [Fact]
    public async Task Handle_WithPendingTransaction_ReturnsFailure()
    {
        var transaction = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var command = new RefundPaymentCommand(transaction.Id, "reason");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Can only refund completed transactions");
    }

    [Fact]
    public async Task Handle_WithFailedTransaction_ReturnsFailure()
    {
        var transaction = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);
        transaction.MarkFailed("Card declined");

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var command = new RefundPaymentCommand(transaction.Id, "reason");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Can only refund completed transactions");
    }

    [Fact]
    public async Task Handle_WithAlreadyRefundedTransaction_ReturnsFailure()
    {
        var transaction = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 100m);
        transaction.MarkCompleted("txn_123");

        var existingRefund = Refund.Create(transaction.Id, transaction.OrderId, 100m, "first refund");
        existingRefund.MarkProcessed("ref_existing");

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _refundRepoMock
            .Setup(r => r.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund> { existingRefund });

        var command = new RefundPaymentCommand(transaction.Id, "second attempt");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("exceed");
    }

    [Fact]
    public async Task Handle_SuccessfulRefund_SetsCorrectRefundFields()
    {
        var orderId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(orderId, "buyer-1", 75.50m);
        transaction.MarkCompleted("txn_456");

        Refund? capturedRefund = null;
        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _refundRepoMock
            .Setup(r => r.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        _refundRepoMock
            .Setup(r => r.Add(It.IsAny<Refund>()))
            .Callback<Refund>(r => capturedRefund = r);

        var command = new RefundPaymentCommand(transaction.Id, "Item defective");

        await _handler.Handle(command, CancellationToken.None);

        capturedRefund.Should().NotBeNull();
        capturedRefund!.TransactionId.Should().Be(transaction.Id);
        capturedRefund.OrderId.Should().Be(orderId);
        capturedRefund.Amount.Should().Be(75.50m);
        capturedRefund.Reason.Should().Be("Item defective");
        capturedRefund.Status.Should().Be(RefundStatus.Processed);
        capturedRefund.GatewayRefundId.Should().StartWith("ref_");
    }

    [Fact]
    public async Task Handle_PartialRefund_DoesNotMarkTransactionRefunded()
    {
        var orderId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(orderId, "buyer-1", 100m);
        transaction.MarkCompleted("txn_partial");

        Refund? capturedRefund = null;
        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _refundRepoMock
            .Setup(r => r.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund>());

        _refundRepoMock
            .Setup(r => r.Add(It.IsAny<Refund>()))
            .Callback<Refund>(r => capturedRefund = r);

        var command = new RefundPaymentCommand(transaction.Id, "Partial refund", 40m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedRefund.Should().NotBeNull();
        capturedRefund!.Amount.Should().Be(40m);
        transaction.Status.Should().Be(PaymentStatus.Completed); // Not refunded yet
    }

    [Fact]
    public async Task Handle_SecondPartialRefund_ExceedingAmount_ReturnsFailure()
    {
        var orderId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(orderId, "buyer-1", 100m);
        transaction.MarkCompleted("txn_partial2");

        var firstRefund = Refund.Create(transaction.Id, orderId, 80m, "first");
        firstRefund.MarkProcessed("ref_80");

        _transactionRepoMock
            .Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _refundRepoMock
            .Setup(r => r.GetByTransactionIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Refund> { firstRefund });

        var command = new RefundPaymentCommand(transaction.Id, "second", 30m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("exceed");
    }
}
