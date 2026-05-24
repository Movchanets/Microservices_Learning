// Refund entity unit tests.
// Tests creation with valid/invalid data, MarkProcessed and MarkFailed state transitions,
// and guard clauses for Amount > 0.

using FluentAssertions;
using Payment.Domain.Aggregates;
using Payment.Domain.Enumerations;

namespace Payment.UnitTests.Domain;

public class RefundTests
{
    [Fact]
    public void Create_WithValidData_InitializesCorrectly()
    {
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var refund = Refund.Create(transactionId, orderId, 99.99m, "Customer request");

        refund.TransactionId.Should().Be(transactionId);
        refund.OrderId.Should().Be(orderId);
        refund.Amount.Should().Be(99.99m);
        refund.Reason.Should().Be("Customer request");
        refund.Status.Should().Be(RefundStatus.Pending);
        refund.GatewayRefundId.Should().BeNull();
        refund.ProcessedAt.Should().BeNull();
        refund.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        var act = () => Refund.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, "reason");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => Refund.Create(Guid.NewGuid(), Guid.NewGuid(), -5m, "reason");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkProcessed_SetsStatusAndGatewayId()
    {
        var refund = Refund.Create(Guid.NewGuid(), Guid.NewGuid(), 50m, "reason");

        refund.MarkProcessed("ref_abc123");

        refund.Status.Should().Be(RefundStatus.Processed);
        refund.GatewayRefundId.Should().Be("ref_abc123");
        refund.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_SetsStatusAndReason()
    {
        var refund = Refund.Create(Guid.NewGuid(), Guid.NewGuid(), 50m, "original reason");

        refund.MarkFailed("gateway timeout");

        refund.Status.Should().Be(RefundStatus.Failed);
        refund.Reason.Should().Be("gateway timeout");
        refund.ProcessedAt.Should().NotBeNull();
    }
}
