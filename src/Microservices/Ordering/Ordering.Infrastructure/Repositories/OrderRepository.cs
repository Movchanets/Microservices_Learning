using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Aggregates;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure.Repositories;

public sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<List<Order>> GetByBuyerIdAsync(string buyerId, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Order>> GetBySellerIdAsync(string sellerId, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.Items.Any(i => i.SellerId == sellerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public void Add(Order entity)
    {
        dbContext.Orders.Add(entity);
    }

    public void Update(Order entity)
    {
        dbContext.Orders.Update(entity);
    }

    public void Remove(Order entity)
    {
        dbContext.Orders.Remove(entity);
    }
}
