using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Domain.Aggregates;
using MediatR;

namespace Ordering.Application.Commands.CancelOrder;

public sealed class CancelOrderHandler(
    IOrderRepository repository,
    IUnitOfWork uow) : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<bool>.Failure("Order not found");

        order.MarkCancelled(request.Reason);
        repository.Update(order);
        await uow.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
