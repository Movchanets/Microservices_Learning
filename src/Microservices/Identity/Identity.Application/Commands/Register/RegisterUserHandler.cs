using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Commands.Register;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check for duplicate email
        if (await userRepository.ExistsAsync(request.Email, cancellationToken))
            return Result<AuthResponse>.Failure("Email already registered", "DUPLICATE_EMAIL");

        // Create user aggregate
        var hashedPassword = passwordHasher.Hash(request.Password);
        var user = User.Create(
            request.Email,
            hashedPassword,
            request.FirstName,
            request.LastName);

        // Generate tokens
        var accessToken = jwtGenerator.GenerateAccessToken(user);
        var refreshTokenStr = jwtGenerator.GenerateRefreshToken();
        var refreshToken = Identity.Domain.ValueObjects.RefreshToken.Create(refreshTokenStr, TimeSpan.FromDays(7));
        user.SetRefreshToken(refreshToken);

        // Persist
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
