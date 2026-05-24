namespace Payment.Infrastructure.External;

/// <summary>
/// Mock gateway for local development. Always succeeds.
/// Replace with Stripe/PayPal SDK in production.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayResult> ProcessPaymentAsync(
        Guid orderId, decimal amount, string buyerId, CancellationToken ct = default)
    {
        var result = new PaymentGatewayResult(
            IsSuccess: true,
            TransactionId: $"txn_{Guid.NewGuid():N}",
            FailureReason: null);

        return Task.FromResult(result);
    }
}
