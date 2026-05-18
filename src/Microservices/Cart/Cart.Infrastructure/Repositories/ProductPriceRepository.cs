using Cart.Domain.Entities;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Repositories;

public class ProductPriceRepository(CartDbContext dbContext) : IProductPriceRepository
{
    public async Task<ProductPrice?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku, ct);
    }

    public async Task<ProductPrice?> GetByIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
    }

    public async Task UpsertAsync(
        Guid productId, string sku, string name, decimal price, string currency, CancellationToken ct = default)
    {
        // Atomic upsert via PostgreSQL INSERT ... ON CONFLICT DO UPDATE.
        // This eliminates the TOCTOU race between ProductCreatedConsumer and
        // ProductUpdatedConsumer when both fire for the same product during seeding.
        await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "ProductPrices" ("Id", "Sku", "Name", "Price", "Currency", "UpdatedAt")
            VALUES ({productId}, {sku}, {name}, {price}, {currency}, {DateTime.UtcNow})
            ON CONFLICT ("Id") DO UPDATE SET
                "Sku"       = EXCLUDED."Sku",
                "Name"      = EXCLUDED."Name",
                "Price"     = EXCLUDED."Price",
                "Currency"  = EXCLUDED."Currency",
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, ct);
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

