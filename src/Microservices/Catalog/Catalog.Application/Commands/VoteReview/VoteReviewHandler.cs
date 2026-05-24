using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using MediatR;

namespace Catalog.Application.Commands.VoteReview;

public sealed class VoteReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VoteReviewCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        VoteReviewCommand request,
        CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return Result<bool>.Failure("Review not found.", "NOT_FOUND");

        // Check for duplicate vote
        var existingVote = await reviewRepository.GetVoteAsync(request.ReviewId, request.UserId, cancellationToken);
        if (existingVote is not null)
            return Result<bool>.Failure("You have already voted on this review.", "DUPLICATE_VOTE");

        // Record the vote
        var vote = ReviewVote.Create(request.ReviewId, request.UserId, request.IsHelpful);
        reviewRepository.AddVote(vote);

        if (request.IsHelpful)
            review.VoteHelpful();
        else
            review.VoteNotHelpful();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
