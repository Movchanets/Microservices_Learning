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
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
    }

    public async Task<ProductPrice?> GetBySkuIdAsync(Guid skuId, CancellationToken ct = default)
    {
        return await dbContext.ProductPrices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SkuId == skuId, ct);
    }

    public async Task<List<ProductPrice>> GetBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default)
    {
        var idList = skuIds.ToList();
        return await dbContext.ProductPrices
            .AsNoTracking()
            .Where(p => idList.Contains(p.SkuId))
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(
        Guid productId, Guid skuId, string skuCode, string name, decimal price, string currency, Guid storeId, CancellationToken ct = default)
    {
        var existing = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.SkuId == skuId, ct);
        if (existing is not null)
        {
            existing.UpdateDetails(name, price, currency);
        }
        else
        {
            dbContext.ProductPrices.Add(
                ProductPrice.Create(productId, skuId, skuCode, name, price, currency, storeId));
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
