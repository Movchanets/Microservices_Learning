using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

/// <summary>
/// Handles the refresh token operation.
/// </summary>
public sealed class RefreshTokenHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    /// <summary>
    /// Processes the refresh token command.
    /// Rationale: Generates a new access token without requiring user credentials, using a valid refresh token.
    /// Note: This is currently a stub and needs a full implementation parsing the expired JWT.
    /// </summary>
    /// <param name="command">The command containing the expired access token and valid refresh token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result containing the new authentication response.</returns>
    public Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        _ = userRepository;
        _ = unitOfWork;
        _ = jwtGenerator;
        // As a mock/placeholder to fix the missing handler for compilation/testing:
        throw new NotImplementedException("RefreshToken handler is not fully implemented");
    }
}
