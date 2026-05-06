using Identity.Application.DTOs;
using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Identity.Application.Commands.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<Result<AuthResponse>>;
