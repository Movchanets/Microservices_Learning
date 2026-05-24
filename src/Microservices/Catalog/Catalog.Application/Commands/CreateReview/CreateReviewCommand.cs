using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.CreateReview;

public sealed record CreateReviewCommand(
    Guid ProductId,
    int Rating,
    string Title,
    string Text,
    List<string>? PhotoUrls = null) : IRequest<Result<ReviewDto>>
{
    // Set server-side from auth claims — not client-supplied
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
}
