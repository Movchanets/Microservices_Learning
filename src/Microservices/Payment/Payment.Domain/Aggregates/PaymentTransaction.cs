using BuildingBlocks.SharedContracts.Abstractions;
using Payment.Domain.Enumerations;

namespace Payment.Domain.Aggregates;

public sealed class PaymentTransaction : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public string BuyerId
    {
        get => field;
        private init => field = !string.IsNullOrWhiteSpace(value)
            ? value : throw new ArgumentException("BuyerId required");
    }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction Create(Guid orderId, string buyerId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

        return new PaymentTransaction
        {
            OrderId = orderId,
            BuyerId = buyerId,
            Amount = amount,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkCompleted(string transactionId)
    {
        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAt = DateTime.UtcNow;
    }
}
