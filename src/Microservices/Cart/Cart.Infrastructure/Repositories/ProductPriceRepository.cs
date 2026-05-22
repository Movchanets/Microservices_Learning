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
        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (existing is not null)
        {
            existing.UpdateDetails(name, price, currency);
        }
        else
        {
            dbContext.ProductPrices.Add(
                ProductPrice.Create(productId, sku, name, price, currency, storeId));
        }

        await dbContext.SaveChangesAsync(ct);
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
