using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Catalog.Application.Commands.VoteReview;

public sealed record VoteReviewCommand(
    Guid ReviewId,
    bool IsHelpful) : IRequest<Result<bool>>
{
    // Set server-side from auth claims — not client-supplied
    public Guid UserId { get; init; }
}
