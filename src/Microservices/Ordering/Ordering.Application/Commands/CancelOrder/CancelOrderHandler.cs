using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;
using MediatR;

namespace Ordering.Application.Commands.CancelOrder;

/// <summary>
/// Handles CancelOrderCommand: validates order exists and is in a cancellable state,
/// publishes CancelOrderEvent for the saga to orchestrate compensation
/// (inventory release + payment refund).
/// </summary>
public sealed class CancelOrderHandler(
    IOrderRepository repository,
    IPublishEndpoint publishEndpoint) : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<bool>.Failure("Order not found");

        // Validate: only Submitted/InventoryReserved/PaymentProcessing can be cancelled.
        // This is a best-effort fast-fail — the saga's During() clause is the real guard.
        // Small race window: if the saga transitions to Completed between this read and
        // the publish below, the saga silently drops the event. The caller receives
        // Success(true) meaning "cancellation requested", not "cancelled". The
        // OrderCancelledProjectionConsumer handles the actual read model update.
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Faulted)
            return Result<bool>.Failure($"Cannot cancel order in {order.Status} state");

        // Publish event to saga — saga handles compensation (inventory release, etc.)
        await publishEndpoint.Publish(new CancelOrderEvent(
            order.Id,
            order.Id,
            order.BuyerId,
            request.Reason,
            DateTime.UtcNow), ct);

        return Result<bool>.Success(true);
    }
}
