using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;
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
            .Where(p => p.Id == id && p.Status != ProductStatus.Deleted)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Sku.Value,
                p.CategoryId,
                p.Category != null ? p.Category.Name : "",
                p.Status.ToString(),
                p.ImageUrl,
                p.StoreId,
                p.Tags,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var skuVo = Sku.Create(sku);
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Sku == skuVo && p.Status != ProductStatus.Deleted)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Sku.Value,
                p.CategoryId,
                p.Category != null ? p.Category.Name : "",
                p.Status.ToString(),
                p.ImageUrl,
                p.StoreId,
                p.Tags,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ProductListDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => idSet.Contains(p.Id) && p.Status != ProductStatus.Deleted)
            .Select(p => new ProductListDto(
                p.Id,
                p.Name,
                p.Price.Amount,
                p.Price.Currency,
                p.Sku.Value,
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
        CancellationToken ct = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

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
                p.Price.Amount,
                p.Price.Currency,
                p.Sku.Value,
                p.Category != null ? p.Category.Name : "",
                p.Status.ToString(),
                p.ImageUrl,
                p.StoreId,
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProductListDto>(items, totalCount, page, pageSize);
    }
}
