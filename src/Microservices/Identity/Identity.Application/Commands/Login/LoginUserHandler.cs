using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Commands.Login;

/// <summary>
/// Handles the login process for an existing user.
/// </summary>
public sealed class LoginUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
{
    /// <summary>
    /// Processes the login command.
    /// Rationale: Validates credentials, checks if the account is active, and issues a new pair of access/refresh tokens.
    /// A generic "Invalid credentials" message is returned to prevent user enumeration attacks.
    /// </summary>
    /// <param name="command">The login command containing credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result containing the authentication response if successful.</returns>
    public async Task<Result<AuthResponse>> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Invalid credentials", "INVALID_CREDENTIALS");

        if (!passwordHasher.Verify(command.Password, user.PasswordHash.Hash))
            return Result<AuthResponse>.Failure("Invalid credentials", "INVALID_CREDENTIALS");

        // Generate new session tokens
        var accessToken = jwtGenerator.GenerateAccessToken(user);
        var refreshTokenStr = jwtGenerator.GenerateRefreshToken();
        var refreshToken = Identity.Domain.ValueObjects.RefreshToken.Create(refreshTokenStr, TimeSpan.FromDays(7));

        user.SetRefreshToken(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshTokenStr,
            DateTime.UtcNow.AddHours(1),
            user.Email.Value,
            user.Role.ToString()));
    }
}
