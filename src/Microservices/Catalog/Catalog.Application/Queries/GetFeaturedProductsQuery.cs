using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record GetFeaturedProductsQuery(string? Tag = null) : IRequest<List<ProductListDto>>;
