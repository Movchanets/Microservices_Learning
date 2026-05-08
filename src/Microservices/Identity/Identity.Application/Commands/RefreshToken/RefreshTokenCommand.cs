using Identity.Application.DTOs;
using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : IRequest<Result<AuthResponse>>;
