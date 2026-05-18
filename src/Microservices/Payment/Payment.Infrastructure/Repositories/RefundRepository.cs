using Microsoft.EntityFrameworkCore;
using Payment.Domain.Aggregates;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public sealed class RefundRepository(PaymentDbContext dbContext) : IRefundRepository
{
    public async Task<Refund?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Refunds.FindAsync([id], cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await dbContext.Refunds
            .Where(r => r.OrderId == orderId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Refund>> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default)
    {
        return await dbContext.Refunds
            .Where(r => r.TransactionId == transactionId)
            .ToListAsync(ct);
    }

    public void Add(Refund refund)
    {
        dbContext.Refunds.Add(refund);
    }
}
