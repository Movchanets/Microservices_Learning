using BuildingBlocks.SharedContracts.Abstractions;
using Payment.Domain.Enumerations;

namespace Payment.Domain.Aggregates;

public sealed class Refund : Entity
{
    public Guid TransactionId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public RefundStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? GatewayRefundId { get; private set; }

    private Refund() { }

    public static Refund Create(Guid transactionId, Guid orderId, decimal amount, string reason)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

        return new Refund
        {
            TransactionId = transactionId,
            OrderId = orderId,
            Amount = amount,
            Reason = reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed(string gatewayRefundId)
    {
        Status = RefundStatus.Processed;
        GatewayRefundId = gatewayRefundId;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RefundStatus.Failed;
        Reason = reason;
        ProcessedAt = DateTime.UtcNow;
    }
}
