using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class CategoryRepository(CatalogDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category?> GetWithAttributeDefinitionsAsync(Guid id, CancellationToken ct = default) =>
        await context.Categories
            .Include(c => c.AttributeDefinitions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Category>> GetAllAsync(CancellationToken ct = default) =>
        await context.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<List<Category>> GetActiveAsync(CancellationToken ct = default) =>
        await context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await context.Categories.AnyAsync(c => c.Id == id, ct);

    public void Add(Category category) => context.Categories.Add(category);
    public void Update(Category category) => context.Categories.Update(category);
}
