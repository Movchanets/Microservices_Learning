using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.Aggregates;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
}
