// PaymentTransaction aggregate unit tests.
// Tests creation with valid/invalid data, MarkCompleted and MarkFailed state transitions,
// and guard clauses for Amount > 0 and non-empty BuyerId.

using FluentAssertions;
using Payment.Domain.Aggregates;
using Payment.Domain.Enumerations;

namespace Payment.UnitTests.Domain;

public class PaymentTransactionTests
{
    [Fact]
    public void Create_WithValidData_InitializesCorrectly()
    {
        var orderId = Guid.NewGuid();
        var txn = PaymentTransaction.Create(orderId, "buyer-1", 99.99m);

        txn.OrderId.Should().Be(orderId);
        txn.BuyerId.Should().Be("buyer-1");
        txn.Amount.Should().Be(99.99m);
        txn.Status.Should().Be(PaymentStatus.Pending);
        txn.TransactionId.Should().BeNull();
        txn.FailureReason.Should().BeNull();
        txn.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        var act = () => PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", -5m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidBuyerId_Throws(string? buyerId)
    {
        var act = () => PaymentTransaction.Create(Guid.NewGuid(), buyerId!, 10m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkCompleted_SetsStatusAndTransactionId()
    {
        var txn = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);

        txn.MarkCompleted("txn_abc123");

        txn.Status.Should().Be(PaymentStatus.Completed);
        txn.TransactionId.Should().Be("txn_abc123");
        txn.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_SetsStatusAndReason()
    {
        var txn = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);

        txn.MarkFailed("insufficient funds");

        txn.Status.Should().Be(PaymentStatus.Failed);
        txn.FailureReason.Should().Be("insufficient funds");
        txn.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkRefunded_WhenCompleted_SetsStatusToRefunded()
    {
        var txn = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);
        txn.MarkCompleted("txn_123");

        txn.MarkRefunded();

        txn.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_WhenPending_Throws()
    {
        var txn = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);

        var act = () => txn.MarkRefunded();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Can only refund completed transactions");
    }

    [Fact]
    public void MarkRefunded_WhenFailed_Throws()
    {
        var txn = PaymentTransaction.Create(Guid.NewGuid(), "buyer-1", 50m);
        txn.MarkFailed("Card declined");

        var act = () => txn.MarkRefunded();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Can only refund completed transactions");
    }
}
