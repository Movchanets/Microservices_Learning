using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Repositories;

public class ProductPriceRepository(CartDbContext dbContext) : IProductPriceRepository
{
    public async Task<ProductPrice?> GetByIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
    }

    public async Task UpsertAsync(
        Guid productId, string sku, string name, decimal price, string currency, Guid storeId, CancellationToken ct = default)
    {
        // Pure EF Core upsert — no raw SQL. Handles TOCTOU race between
        // ProductCreatedConsumer and ProductUpdatedConsumer via catch-and-retry.
        dbContext.ChangeTracker.Clear();

        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (existing is not null)
        {
            existing.UpdateDetails(name, price, currency);
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        dbContext.ProductPrices.Add(
            ProductPrice.Create(productId, sku, name, price, currency, storeId));

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (ct.IsCancellationRequested is false)
        {
            // Race: another consumer inserted between our check and our save.
            // Detach the failed entity, load the winner, update it.
            dbContext.ChangeTracker.Clear();
            var retry = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (retry is not null)
            {
                retry.UpdateDetails(name, price, currency);
                await dbContext.SaveChangesAsync(ct);
            }
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    public void Add(ProductPrice productPrice)
    {
        dbContext.ProductPrices.Add(productPrice);
    }

    public void Update(ProductPrice productPrice)
    {
        dbContext.ProductPrices.Update(productPrice);
    }

    public void Remove(ProductPrice productPrice)
    {
        dbContext.ProductPrices.Remove(productPrice);
    }
}
