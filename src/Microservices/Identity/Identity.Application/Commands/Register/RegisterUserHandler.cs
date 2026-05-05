using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Commands.Register;

/// <summary>
/// Handles the registration of a new user.
/// </summary>
public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    /// <summary>
    /// Processes the register user command.
    /// Rationale: Registration requires ensuring the email is unique, creating the User aggregate, securely hashing the password, and establishing an initial auth session.
    /// </summary>
    /// <param name="command">The registration command containing user details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result containing the authentication response with access and refresh tokens if successful.</returns>
    public async Task<Result<AuthResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // Check for duplicate email to maintain invariants
        if (await userRepository.ExistsAsync(command.Email, cancellationToken))
            return Result<AuthResponse>.Failure("Email already registered", "DUPLICATE_EMAIL");

        // Rationale: Hash the password using the infrastructure service before saving
        var hashedPassword = passwordHasher.Hash(command.Password);
        var user = User.Create(
            command.Email,
            hashedPassword,
            command.FirstName,
            command.LastName);

        // Generate an initial set of tokens for immediate login post-registration
        var accessToken = jwtGenerator.GenerateAccessToken(user);
        var refreshTokenStr = jwtGenerator.GenerateRefreshToken();
        var refreshToken = Identity.Domain.ValueObjects.RefreshToken.Create(refreshTokenStr, TimeSpan.FromDays(7));

        user.SetRefreshToken(refreshToken);

        // Persist the new user
        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshTokenStr,
            DateTime.UtcNow.AddHours(1),
            user.Email.Value,
            user.Role.ToString()));
    }
}
