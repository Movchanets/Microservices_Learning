using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record GetProductRecommendationsQuery(Guid ProductId) : IRequest<List<ProductListDto>>;
