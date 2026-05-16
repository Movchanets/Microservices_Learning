using Catalog.Domain.Aggregates;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != ProductStatus.Deleted, ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Sku.Value == sku && p.Status != ProductStatus.Deleted, ct);

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default) =>
        await context.Products.AnyAsync(p => p.Sku.Value == sku && p.Status != ProductStatus.Deleted, ct);

    public async Task<List<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await context.Products
            .Where(p => p.CategoryId == categoryId && p.Status != ProductStatus.Deleted)
            .ToListAsync(ct);

    public async Task<List<Product>> GetByStoreAsync(Guid storeId, CancellationToken ct = default) =>
        await context.Products
            .Where(p => p.StoreId == storeId && p.Status != ProductStatus.Deleted)
            .ToListAsync(ct);

    public void Add(Product product) => context.Products.Add(product);

    public void Update(Product product) => context.Products.Update(product);

    public void Remove(Product product) => context.Products.Remove(product);
}
