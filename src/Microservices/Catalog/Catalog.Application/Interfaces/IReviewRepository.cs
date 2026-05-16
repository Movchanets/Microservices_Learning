using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;

namespace Catalog.Application.Interfaces;

public interface IReviewRepository
{
    void Add(Review review);
    Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Review?> GetByProductAndUserAsync(Guid productId, Guid userId, CancellationToken ct = default);
    Task<PagedResult<Review>> GetByProductAsync(
        Guid productId,
        int page,
        int pageSize,
        string sort,
        int? ratingFilter,
        bool? photoOnly,
        CancellationToken ct = default);
    Task<ReviewSummaryDto> GetSummaryAsync(Guid productId, CancellationToken ct = default);
    Task<ReviewVote?> GetVoteAsync(Guid reviewId, Guid userId, CancellationToken ct = default);
    void AddVote(ReviewVote vote);
}
