using Identity.Application.DTOs;
using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Identity.Application.Commands.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password) : IRequest<Result<AuthResponse>>;
