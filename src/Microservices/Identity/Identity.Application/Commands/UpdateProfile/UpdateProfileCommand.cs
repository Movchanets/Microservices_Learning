using BuildingBlocks.Infrastructure.Models;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email) : IRequest<Result<Guid>>;
