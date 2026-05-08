using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Queries;

/// <summary>
/// Handles querying a user by their unique identifier.
/// </summary>
public sealed class GetUserByIdHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    /// <summary>
    /// Processes the query to retrieve a user.
    /// Rationale: Separates read operations (Queries) from write operations (Commands). Maps the domain aggregate to a safe DTO before returning.
    /// </summary>
    /// <param name="query">The query containing the user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A UserDto containing user details, or null if the user was not found.</returns>
    public async Task<UserDto?> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
            return null;

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt);
    }
}
