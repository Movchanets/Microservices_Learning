using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        // Retry loop handles concurrent inserts from ProductCreatedConsumer
        // and ProductUpdatedConsumer (both fire on Catalog restart due to
        // outbox replay + seeder emitting ProductUpdatedEvent).
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
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
                return; // Success
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // Race: another consumer inserted between our check and save.
                // Clear tracker IMMEDIATELY so the EF Outbox doesn't re-flush
                // the stale Added entity when it calls SaveChangesAsync after
                // this consumer returns.
                dbContext.ChangeTracker.Clear();

                if (attempt == maxRetries - 1)
                {
                    // Last attempt — load the winner and update it
                    var fallback = await dbContext.ProductPrices.FirstOrDefaultAsync(p => p.Id == productId, ct);
                    if (fallback is not null)
                    {
                        fallback.UpdateDetails(name, price, currency);
                        await dbContext.SaveChangesAsync(ct);
                    }
                    return;
                }
                // Otherwise loop back — next iteration will clear tracker again and re-check
            }
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
