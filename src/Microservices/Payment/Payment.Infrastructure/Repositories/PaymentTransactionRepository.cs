using Microsoft.EntityFrameworkCore;
using Payment.Domain.Aggregates;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for PaymentTransaction aggregate roots.
/// Supports lookup by ID and by OrderId for saga correlation.
/// </summary>
public sealed class PaymentTransactionRepository(PaymentDbContext dbContext) : IPaymentTransactionRepository
{
    public async Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.PaymentTransactions.FindAsync([id], cancellationToken: ct);
    }

    public async Task<PaymentTransaction?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await dbContext.PaymentTransactions
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
    }

    public void Add(PaymentTransaction entity)
    {
        dbContext.PaymentTransactions.Add(entity);
    }

    public void Update(PaymentTransaction entity)
    {
        dbContext.PaymentTransactions.Update(entity);
    }

    public void Remove(PaymentTransaction entity)
    {
        dbContext.PaymentTransactions.Remove(entity);
    }
}
