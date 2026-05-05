using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Commands.Login;

public sealed class LoginUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Invalid credentials", "INVALID_CREDENTIALS");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash.Hash))
            return Result<AuthResponse>.Failure("Invalid credentials", "INVALID_CREDENTIALS");

        // Generate tokens
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
