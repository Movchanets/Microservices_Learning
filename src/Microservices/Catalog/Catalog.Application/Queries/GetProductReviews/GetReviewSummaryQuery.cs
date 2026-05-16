using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries.GetProductReviews;

public sealed record GetReviewSummaryQuery(Guid ProductId) : IRequest<ReviewSummaryDto>;
