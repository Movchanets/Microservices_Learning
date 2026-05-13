using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Enums;
using Catalog.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Models;
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
                p.SellerId,
                p.Tags,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<ProductListDto>> ListAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        Guid? sellerId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (sellerId.HasValue)
            query = query.Where(p => p.SellerId == sellerId.Value);

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
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProductListDto>(items, totalCount, page, pageSize);
    }
}
