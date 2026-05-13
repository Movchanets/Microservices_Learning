using Catalog.Domain.Aggregates;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Sku == Catalog.Domain.ValueObjects.Sku.Create(sku), ct);

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default) =>
        await context.Products.AnyAsync(
            p => p.Sku == Catalog.Domain.ValueObjects.Sku.Create(sku), ct);

    public async Task<List<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(ct);

    public async Task<List<Product>> GetBySellerAsync(Guid sellerId, CancellationToken ct = default) =>
        await context.Products
            .Include(p => p.Category)
            .Where(p => p.SellerId == sellerId)
            .ToListAsync(ct);

    public void Add(Product entity) => context.Products.Add(entity);
    public void Update(Product entity) => context.Products.Update(entity);
    public void Remove(Product entity) => context.Products.Remove(entity);
}
