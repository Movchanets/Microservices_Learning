using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    Guid? StoreId = null,
    string? Search = null,
    string? Status = null) : IRequest<PagedResult<ProductListDto>>;
