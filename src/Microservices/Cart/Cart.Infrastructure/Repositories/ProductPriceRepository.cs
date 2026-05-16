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
            .FirstOrDefaultAsync(p => p.Sku == sku, ct);
    }

    public async Task<ProductPrice?> GetByIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
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
