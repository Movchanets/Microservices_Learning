using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries;

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
