using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Queries.GetProductReviews;

public sealed class GetProductReviewsHandler(
    IReviewRepository reviewRepository)
    : IRequestHandler<GetProductReviewsQuery, PagedResult<ReviewDto>>
{
    public async Task<PagedResult<ReviewDto>> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await reviewRepository.GetByProductAsync(
            request.ProductId,
            request.Page,
            request.PageSize,
            request.Sort,
            request.RatingFilter,
            request.PhotoOnly,
            cancellationToken);

        var dtos = result.Items.Select(r => new ReviewDto(
            r.Id, r.UserId, r.UserName, r.Rating, r.Title, r.Text,
            r.IsVerifiedPurchase, r.PhotoUrls, r.HelpfulCount, r.NotHelpfulCount,
            r.SellerResponse, r.SellerResponseDate, r.CreatedAt)).ToList();

        return new PagedResult<ReviewDto>(dtos, result.TotalCount, request.Page, request.PageSize);
    }
}
