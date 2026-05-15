using FluentAssertions;
using Payment.Infrastructure.External;

namespace Payment.UnitTests.Infrastructure;

public class MockPaymentGatewayTests
{
    private readonly MockPaymentGateway _gateway = new();

    [Fact]
    public async Task ProcessPaymentAsync_AlwaysReturnsSuccess()
    {
        var result = await _gateway.ProcessPaymentAsync(
            Guid.NewGuid(), 100m, "buyer-1");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessPaymentAsync_ReturnsTransactionIdWithPrefix()
    {
        var result = await _gateway.ProcessPaymentAsync(
            Guid.NewGuid(), 100m, "buyer-1");

        result.TransactionId.Should().StartWith("txn_");
        result.TransactionId.Should().HaveLength(36); // "txn_" + 32 hex chars
    }

    [Fact]
    public async Task ProcessPaymentAsync_ReturnsNullFailureReason()
    {
        var result = await _gateway.ProcessPaymentAsync(
            Guid.NewGuid(), 100m, "buyer-1");

        result.FailureReason.Should().BeNull();
    }
}
