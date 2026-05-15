using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Domain.Aggregates;
using MediatR;

namespace Ordering.Application.Commands.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IUnitOfWork uow) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.BuyerId);

        foreach (var item in request.Items)
        {
            order.AddItem(item.Sku, item.ProductName, item.UnitPrice, item.Quantity);
        }

        repository.Add(order);
        await uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id);
    }
}
