namespace Payment.Domain.Aggregates;

public interface IRefundRepository
{
    Task<Refund?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Refund>> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default);
    void Add(Refund refund);
}
