using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted))
            .Include(p => p.VariantAxes)
                .ThenInclude(va => va.AttributeDefinition)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != ProductStatus.Deleted, ct);

    public async Task<Product?> GetWithSkusAsync(Guid productId, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted))
            .Include(p => p.VariantAxes)
                .ThenInclude(va => va.AttributeDefinition)
            .FirstOrDefaultAsync(p => p.Id == productId && p.Status != ProductStatus.Deleted, ct);

    public async Task<Sku?> GetSkuByCodeAsync(string skuCode, CancellationToken ct = default)
    {
        var normalized = skuCode.Trim().ToUpperInvariant();
        return await context.Skus
            .FirstOrDefaultAsync(s => s.SkuCode == normalized && s.Status != SkuStatus.Deleted, ct);
    }

    public async Task<bool> ExistsBySkuCodeAsync(string skuCode, CancellationToken ct = default)
    {
        var normalized = skuCode.Trim().ToUpperInvariant();
        return await context.Skus.AnyAsync(s => s.SkuCode == normalized && s.Status != SkuStatus.Deleted, ct);
    }

    public async Task<List<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted))
            .Where(p => p.CategoryId == categoryId && p.Status != ProductStatus.Deleted)
            .ToListAsync(ct);

    public async Task<List<Product>> GetByStoreAsync(Guid storeId, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted))
            .Where(p => p.StoreId == storeId && p.Status != ProductStatus.Deleted)
            .ToListAsync(ct);

    public void Add(Product product) => context.Products.Add(product);

    public void Update(Product product) => context.Products.Update(product);

    public void Remove(Product product) => context.Products.Remove(product);
}
