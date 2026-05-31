using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Ordering.Domain.Aggregates;
using Ordering.Domain.ValueObjects;
using MediatR;

namespace Ordering.Application.Commands.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IUnitOfWork uow) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var address = Address.FromShipping(
            request.ShippingAddressLine1, request.ShippingAddressLine2,
            request.ShippingCity, request.ShippingState,
            request.ShippingPostalCode, request.ShippingCountry);

        var order = Order.Create(request.BuyerId, address);

        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.SkuId, item.SkuCode, item.ProductName, item.UnitPrice, item.Quantity, item.StoreId);
        }

        repository.Add(order);
        await uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id);
    }
}
