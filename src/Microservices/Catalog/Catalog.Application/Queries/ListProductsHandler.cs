using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

/// <summary>
/// Handles ListProductsQuery: delegates to the read-only repository for paginated
/// product listing with optional filtering by store, category, and status.
/// Uses AsNoTracking() projections for optimal read performance.
/// </summary>
public sealed class ListProductsHandler(
    IProductReadRepository readRepository)
    : IRequestHandler<ListProductsQuery, PagedResult<ProductListDto>>
{
    public async Task<PagedResult<ProductListDto>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken) =>
        await readRepository.ListAsync(
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.StoreId,
            request.Search,
            request.Status,
            cancellationToken);
}
