using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using MediatR;

namespace Catalog.Application.Commands.CreateReview;

public sealed class CreateReviewHandler(
    IProductReadRepository readRepository,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    public async Task<Result<ReviewDto>> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        // Verify product exists
        var product = await readRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<ReviewDto>.Failure("Product not found.", "NOT_FOUND");

        // Check user hasn't already reviewed this product
        var existing = await reviewRepository.GetByProductAndUserAsync(
            request.ProductId, request.UserId, cancellationToken);
        if (existing is not null)
            return Result<ReviewDto>.Failure("You have already reviewed this product.", "DUPLICATE_REVIEW");

        // Create review
        // TODO: Check Ordering.API for verified purchase (buyerId + productId)
        var isVerifiedPurchase = false;
        var review = Review.Create(
            request.ProductId,
            request.UserId,
            request.UserName,
            request.Rating,
            request.Title,
            request.Text,
            isVerifiedPurchase,
            request.PhotoUrls);

        reviewRepository.Add(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReviewDto>.Success(MapToDto(review));
    }

    private static ReviewDto MapToDto(Review r) => new(
        r.Id, r.UserId, r.UserName, r.Rating, r.Title, r.Text,
        r.IsVerifiedPurchase, r.PhotoUrls, r.HelpfulCount, r.NotHelpfulCount,
        r.SellerResponse, r.SellerResponseDate, r.CreatedAt);
}
