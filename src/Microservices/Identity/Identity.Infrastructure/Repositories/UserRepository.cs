using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

/// <summary>
/// Provides data access operations for the User aggregate using Entity Framework Core.
/// </summary>
public sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <inheritdoc/>
    /// <remarks>
    /// Rationale: Reconstructs the Email value object to leverage EF Core's value conversion when matching against the database column.
    /// </remarks>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users.FirstOrDefaultAsync(
            u => u.Email == Identity.Domain.ValueObjects.Email.Create(email), ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default) =>
        await context.Users.AnyAsync(
            u => u.Email == Identity.Domain.ValueObjects.Email.Create(email), ct);

    /// <inheritdoc/>
    public void Add(User entity) => context.Users.Add(entity);

    /// <inheritdoc/>
    public void Update(User entity) => context.Users.Update(entity);

    /// <inheritdoc/>
    public void Remove(User entity) => context.Users.Remove(entity);
}
