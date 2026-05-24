using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Ordering;
using MassTransit;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;
using MediatR;

namespace Ordering.Application.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler(
    IOrderRepository repository,
    IUnitOfWork uow,
    IPublishEndpoint publishEndpoint) : IRequestHandler<UpdateOrderStatusCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<bool>.Failure("Order not found");

        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var newStatus))
            return Result<bool>.Failure($"Invalid status: {request.Status}");

        try
        {
            order.UpdateStatus(newStatus, request.Notes);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        repository.Update(order);
        await uow.SaveChangesAsync(ct);

        // Publish integration event for notifications
        await publishEndpoint.Publish(new OrderStatusChangedEvent(
            order.Id,
            order.BuyerId,
            newStatus.ToString(),
            request.Notes,
            DateTime.UtcNow), ct);

        return Result<bool>.Success(true);
    }
}
