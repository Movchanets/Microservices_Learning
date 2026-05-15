namespace Payment.Infrastructure.External;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(
        Guid orderId, decimal amount, string buyerId, CancellationToken ct = default);
}

public sealed record PaymentGatewayResult(
    bool IsSuccess,
    string? TransactionId,
    string? FailureReason);
