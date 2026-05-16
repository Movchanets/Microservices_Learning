using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Identity.Application.Commands.DeleteSavedSearch;

public sealed record DeleteSavedSearchCommand(
    Guid SearchId,
    Guid UserId) : IRequest<Result<bool>>;
