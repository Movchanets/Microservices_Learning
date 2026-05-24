using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Ordering.Application.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    string Status,
    string? Notes) : IRequest<Result<bool>>;
