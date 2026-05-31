using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Media.API.Application.Commands.DeleteMedia;

public sealed record DeleteMediaCommand(Guid MediaItemId) : IRequest<Result<bool>>;
