using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users.FirstOrDefaultAsync(
            u => u.Email == Identity.Domain.ValueObjects.Email.Create(email), ct);

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default) =>
        await context.Users.AnyAsync(
            u => u.Email == Identity.Domain.ValueObjects.Email.Create(email), ct);

    public void Add(User entity) => context.Users.Add(entity);
    public void Update(User entity) => context.Users.Update(entity);
    public void Remove(User entity) => context.Users.Remove(entity);
}
