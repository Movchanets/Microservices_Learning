using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ProductReadRepository(CatalogDbContext context) : IProductReadRepository
{
    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted))
            .Where(p => p.Id == id && p.Status != ProductStatus.Deleted)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.CategoryId,
                p.Category != null ? p.Category.Name : "",
                p.Status.ToString(),
                p.ImageUrl,
                p.Brand,
                p.StoreId,
                p.Tags,
                p.Skus
                    .Where(s => s.Status != SkuStatus.Deleted)
                    .Select(s => new SkuDto(
                        s.Id,
                        s.SkuCode,
                        s.Price.Amount,
                        s.Price.Currency,
                        s.Status.ToString(),
                        s.ImageUrl,
                        s.TypedAttributes,
                        s.FlexibleAttributes,
                        s.CreatedAt))
                    .ToList(),
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SkuDto?> GetSkuByIdAsync(Guid skuId, CancellationToken ct = default)
    {
        return await context.Skus
            .AsNoTracking()
            .Where(s => s.Id == skuId && s.Status != SkuStatus.Deleted)
            .Select(s => new SkuDto(
                s.Id,
                s.SkuCode,
                s.Price.Amount,
                s.Price.Currency,
                s.Status.ToString(),
                s.ImageUrl,
                s.TypedAttributes,
                s.FlexibleAttributes,
                s.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return await context.Skus
            .AsNoTracking()
            .Where(s => s.SkuCode == normalized && s.Status != SkuStatus.Deleted)
            .Select(s => s.ProductId)
            .FirstOrDefaultAsync(ct) is Guid productId
            ? await GetByIdAsync(productId, ct)
            : null;
    }

    public async Task<List<ProductListDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted))
            .Where(p => idSet.Contains(p.Id) && p.Status != ProductStatus.Deleted)
            .Select(p => new ProductListDto(
                p.Id,
                p.Name,
                p.Skus.Where(s => s.Status == SkuStatus.Active).Min(s => (decimal?)s.Price.Amount),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Max(s => (decimal?)s.Price.Amount),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Select(s => s.Price.Currency).FirstOrDefault(),
                p.Skus.Count(s => s.Status == SkuStatus.Active),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Select(s => (Guid?)s.Id).FirstOrDefault(),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Select(s => s.SkuCode).FirstOrDefault(),
                p.Category != null ? p.Category.Name : "",
                p.Status.ToString(),
                p.ImageUrl,
                p.StoreId,
                p.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<ProductListDto>> ListAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        Guid? storeId = null,
        string? search = null,
        string? status = null,
        CancellationToken ct = default)
    {
        IQueryable<Product> query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Skus.Where(s => s.Status != SkuStatus.Deleted));

        // Status filter: null/empty/"Active" → Active only; "All" → all non-deleted; specific status → filter by it
        if (string.IsNullOrWhiteSpace(status) || status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Status == ProductStatus.Active);
        }
        else if (status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Status != ProductStatus.Deleted);
        }
        else if (Enum.TryParse<ProductStatus>(status, true, out var parsed))
        {
            query = query.Where(p => p.Status == parsed);
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Description.Contains(search));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListDto(
                p.Id,
                p.Name,
                p.Skus.Where(s => s.Status == SkuStatus.Active).Min(s => (decimal?)s.Price.Amount),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Max(s => (decimal?)s.Price.Amount),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Select(s => s.Price.Currency).FirstOrDefault(),
                p.Skus.Count(s => s.Status == SkuStatus.Active),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Select(s => (Guid?)s.Id).FirstOrDefault(),
                p.Skus.Where(s => s.Status == SkuStatus.Active).Select(s => s.SkuCode).FirstOrDefault(),
                p.Category != null ? p.Category.Name : "",
                p.Status.ToString(),
                p.ImageUrl,
                p.StoreId,
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProductListDto>(items, totalCount, page, pageSize);
    }
}
