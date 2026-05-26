using BuildingBlocks.Infrastructure.Models;
using Ordering.Application.DTOs;
using Ordering.Domain.Aggregates;
using MediatR;

namespace Ordering.Application.Queries.ListOrdersByBuyer;

public sealed class ListOrdersByBuyerHandler(
    IOrderRepository repository) : IRequestHandler<ListOrdersByBuyerQuery, Result<List<OrderDto>>>
{
    public async Task<Result<List<OrderDto>>> Handle(ListOrdersByBuyerQuery request, CancellationToken ct)
    {
        var orders = await repository.GetByBuyerIdAsync(request.BuyerId, ct);

        var dtos = orders.Select(order => new OrderDto(
            order.Id,
            order.BuyerId,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.CompletedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id, i.ProductId, i.SkuId, i.SkuCode, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice)).ToList())).ToList();

        return Result<List<OrderDto>>.Success(dtos);
    }
}
