using BuildingBlocks.Infrastructure.Models;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

/// <summary>
/// Handles the refresh token operation.
/// Currently returns a failure result — token refresh is handled by the BFF gateway's session mechanism.
/// </summary>
public sealed class RefreshTokenHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        // Token refresh is handled by the BFF gateway's session/cookie mechanism.
        // This endpoint is not used in the current architecture.
        return Task.FromResult(Result<AuthResponse>.Failure(
            "Token refresh is not supported. Use the BFF login flow.",
            "NOT_SUPPORTED"));
    }
}
