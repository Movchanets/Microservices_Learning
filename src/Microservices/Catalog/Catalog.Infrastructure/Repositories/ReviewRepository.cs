using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ReviewRepository(CatalogDbContext context) : IReviewRepository
{
    public void Add(Review review)
    {
        context.Reviews.Add(review);
    }

    public async Task<Review?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Reviews.FindAsync([id], ct);
    }

    public async Task<Review?> GetByProductAndUserAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        return await context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId, ct);
    }

    public async Task<PagedResult<Review>> GetByProductAsync(
        Guid productId,
        int page,
        int pageSize,
        string sort,
        int? ratingFilter,
        bool? photoOnly,
        CancellationToken ct = default)
    {
        var query = context.Reviews
            .Where(r => r.ProductId == productId)
            .AsNoTracking();

        if (ratingFilter.HasValue)
            query = query.Where(r => r.Rating == ratingFilter.Value);

        if (photoOnly == true)
            query = query.Where(r => r.PhotoUrls.Count > 0);

        query = sort switch
        {
            "newest" => query.OrderByDescending(r => r.CreatedAt),
            "highest" => query.OrderByDescending(r => r.Rating),
            "lowest" => query.OrderBy(r => r.Rating),
            _ => query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Review>(items, totalCount, page, pageSize);
    }

    public async Task<ReviewSummaryDto> GetSummaryAsync(Guid productId, CancellationToken ct = default)
    {
        var reviews = await context.Reviews
            .Where(r => r.ProductId == productId)
            .AsNoTracking()
            .ToListAsync(ct);

        if (reviews.Count == 0)
            return new ReviewSummaryDto(0, 0, new Dictionary<int, int>
            {
                { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
            });

        var distribution = new Dictionary<int, int>
        {
            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
        };
        foreach (var r in reviews)
            distribution[r.Rating]++;

        return new ReviewSummaryDto(
            Math.Round(reviews.Average(r => r.Rating), 1),
            reviews.Count,
            distribution);
    }

    public async Task<ReviewVote?> GetVoteAsync(Guid reviewId, Guid userId, CancellationToken ct = default)
    {
        return await context.ReviewVotes
            .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId, ct);
    }

    public void AddVote(ReviewVote vote)
    {
        context.ReviewVotes.Add(vote);
    }
}
