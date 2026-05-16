using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries.GetProductReviews;

public sealed class GetReviewSummaryHandler(
    IReviewRepository reviewRepository)
    : IRequestHandler<GetReviewSummaryQuery, ReviewSummaryDto>
{
    public async Task<ReviewSummaryDto> Handle(
        GetReviewSummaryQuery request,
        CancellationToken cancellationToken)
    {
        return await reviewRepository.GetSummaryAsync(request.ProductId, cancellationToken);
    }
}
