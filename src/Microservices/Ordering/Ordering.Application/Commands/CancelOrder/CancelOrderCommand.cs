using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Ordering.Application.Commands.CancelOrder;

public sealed record CancelOrderCommand(
    Guid OrderId,
    string Reason) : IRequest<Result<bool>>;
