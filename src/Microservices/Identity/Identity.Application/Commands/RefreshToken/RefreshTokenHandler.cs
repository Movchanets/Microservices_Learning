using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtTokenGenerator jwtGenerator)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        _ = userRepository;
        _ = unitOfWork;
        _ = jwtGenerator;
        // As a mock/placeholder to fix the missing handler for compilation/testing:
        throw new NotImplementedException("RefreshToken handler is not fully implemented");
    }
}
