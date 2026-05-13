using Catalog.Application.DTOs;
using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    Guid? SellerId = null,
    string? Search = null) : IRequest<PagedResult<ProductListDto>>;
