using Microsoft.EntityFrameworkCore;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;
using StoreManagement.Infrastructure.Persistence;

namespace StoreManagement.Infrastructure.Repositories;

public sealed class StoreRepository(StoreDbContext context) : IStoreRepository
{
    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Store?> GetBySellerIdAsync(string sellerId, CancellationToken ct = default) =>
        await context.Stores.FirstOrDefaultAsync(s => s.SellerId == sellerId, ct);

    public async Task<bool> ExistsBySellerIdAsync(string sellerId, CancellationToken ct = default) =>
        await context.Stores.AnyAsync(s => s.SellerId == sellerId, ct);

    public async Task<List<Store>> GetAllAsync(CancellationToken ct = default) =>
        await context.Stores.ToListAsync(ct);

    public void Add(Store store) => context.Stores.Add(store);

    public void Update(Store store) => context.Stores.Update(store);

    public void Remove(Store store) => context.Stores.Remove(store);
}
