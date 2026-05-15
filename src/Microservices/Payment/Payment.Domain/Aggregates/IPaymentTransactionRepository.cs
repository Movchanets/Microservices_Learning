using BuildingBlocks.SharedContracts.Abstractions;

namespace Payment.Domain.Aggregates;

public interface IPaymentTransactionRepository : IRepository<PaymentTransaction>
{
    Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}
