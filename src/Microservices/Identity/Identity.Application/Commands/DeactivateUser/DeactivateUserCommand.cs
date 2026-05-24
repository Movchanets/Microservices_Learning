using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Identity.Application.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<Result<bool>>;
