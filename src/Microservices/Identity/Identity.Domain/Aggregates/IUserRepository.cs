using BuildingBlocks.SharedContracts.Abstractions;

namespace Identity.Domain.Aggregates;

/// <summary>
/// Defines the repository interface for the User aggregate.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user with the specified email address exists.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the user exists, otherwise false.</returns>
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
}
