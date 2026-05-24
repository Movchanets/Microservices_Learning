using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Aggregates;

public class ReviewVote : Entity
{
    public Guid ReviewId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsHelpful { get; private set; }

    private ReviewVote() { }

    public static ReviewVote Create(Guid reviewId, Guid userId, bool isHelpful)
    {
        return new ReviewVote
        {
            ReviewId = reviewId,
            UserId = userId,
            IsHelpful = isHelpful
        };
    }
}
