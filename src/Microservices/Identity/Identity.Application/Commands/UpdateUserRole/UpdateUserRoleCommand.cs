using BuildingBlocks.Infrastructure.Models;
using Identity.Application.Queries;
using MediatR;

namespace Identity.Application.Commands.UpdateUserRole;

public sealed record UpdateUserRoleCommand(
    Guid UserId,
    string Role) : IRequest<Result<UserDto>>;
