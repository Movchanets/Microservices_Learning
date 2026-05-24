using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class SavedSearchRepository(IdentityDbContext context) : ISavedSearchRepository
{
    public void Add(SavedSearch search)
    {
        context.SavedSearches.Add(search);
    }

    public async Task<SavedSearch?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.SavedSearches.FindAsync([id], ct);
    }

    public async Task<List<SavedSearch>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.SavedSearches
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var search = await context.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        if (search is not null)
            context.SavedSearches.Remove(search);
    }
}
