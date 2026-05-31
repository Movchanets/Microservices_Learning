using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Media.API.Application.Commands.SetPrimaryMedia;

public sealed record SetPrimaryMediaCommand(
    Guid TargetId,
    string TargetType,
    Guid MediaItemId) : IRequest<Result<bool>>;
