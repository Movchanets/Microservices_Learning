using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Interfaces;
using MediatR;

namespace Catalog.Application.Commands.SellerResponse;

public sealed class SellerResponseHandler(
    IReviewRepository reviewRepository,
    IProductReadRepository productReadRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SellerResponseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        SellerResponseCommand request,
        CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result<bool>.Failure("Review not found.", "NOT_FOUND");

        // Verify the seller owns the product being reviewed
        var product = await productReadRepository.GetByIdAsync(review.ProductId, cancellationToken);
        if (product is null || product.StoreId != request.StoreId)
            return Result<bool>.Failure("You can only respond to reviews on your own products.", "FORBIDDEN");

        review.AddSellerResponse(request.Response);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
