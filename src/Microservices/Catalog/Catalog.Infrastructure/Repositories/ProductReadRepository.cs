using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

/// <summary>
/// Read-only repository for product queries. Uses EF Core projections to DTOs
/// with AsNoTracking() for optimal read performance. Supports filtering by
/// store, category, status, and full-text search.
/// </summary>
public sealed class ProductReadRepository(CatalogDbContext context) : IProductReadRepository
{
    // ── Queries ──────────────────────────────────────────────────────

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
                p.ImageUrl ?? p.Skus
                    .Where(s => s.Status != SkuStatus.Deleted && s.ImageUrl != null)
                    .Select(s => s.ImageUrl)
                    .FirstOrDefault(),
                p.Brand,
                p.StoreId,
                p.Tags,
                p.Skus
                    .Where(s => s.Status != SkuStatus.Deleted)
                    .Select(s => ToSkuDto(s))
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
            .Select(s => ToSkuDto(s))
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
            .Select(p => ToProductListDto(p))
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

        // ── Filters ─────────────────────────────────────────────────
        query = ApplyStatusFilter(query, status);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Description.Contains(search));

        // ── Pagination ──────────────────────────────────────────────
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToProductListDto(p))
            .ToListAsync(ct);

        return new PagedResult<ProductListDto>(items, totalCount, page, pageSize);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Projects a SKU entity to SkuDto. Must be a static method (not Func delegate)
    /// so EF Core can translate it as an expression tree inside IQueryable.Select().
    /// </summary>
    private static SkuDto ToSkuDto(Sku s) => new(
        s.Id,
        s.SkuCode,
        s.Price.Amount,
        s.Price.Currency,
        s.Status.ToString(),
        s.ImageUrl,
        s.TypedAttributes,
        s.FlexibleAttributes,
        s.CreatedAt);

    /// <summary>
    /// Projects a Product entity to ProductListDto with aggregated SKU data.
    /// Must be a static method (not Func delegate) for EF Core translation.
    /// </summary>
    private static ProductListDto ToProductListDto(Product p) => new(
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
        p.ImageUrl ?? p.Skus
            .Where(s => s.Status == SkuStatus.Active && s.ImageUrl != null)
            .Select(s => s.ImageUrl)
            .FirstOrDefault(),
        p.StoreId,
        p.CreatedAt);

    /// <summary>
    /// Applies status filter to the query. Defaults to Active if not specified.
    /// "All" returns all non-deleted products.
    /// </summary>
    private static IQueryable<Product> ApplyStatusFilter(IQueryable<Product> query, string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            return query.Where(p => p.Status == ProductStatus.Active);

        if (status.Equals("All", StringComparison.OrdinalIgnoreCase))
            return query.Where(p => p.Status != ProductStatus.Deleted);

        if (Enum.TryParse<ProductStatus>(status, true, out var parsed))
            return query.Where(p => p.Status == parsed);

        return query;
    }
}
