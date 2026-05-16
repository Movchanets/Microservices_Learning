using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries.GetProductReviews;

public sealed record GetProductReviewsQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 10,
    string Sort = "helpful",
    int? RatingFilter = null,
    bool? PhotoOnly = null) : IRequest<PagedResult<ReviewDto>>;
