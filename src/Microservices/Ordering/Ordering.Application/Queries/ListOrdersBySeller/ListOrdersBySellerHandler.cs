using BuildingBlocks.Infrastructure.Models;
using Ordering.Application.DTOs;
using Ordering.Domain.Aggregates;
using MediatR;

namespace Ordering.Application.Queries.ListOrdersBySeller;

public sealed class ListOrdersBySellerHandler(
    IOrderRepository repository) : IRequestHandler<ListOrdersBySellerQuery, Result<List<OrderDto>>>
{
    public async Task<Result<List<OrderDto>>> Handle(ListOrdersBySellerQuery request, CancellationToken ct)
    {
        var orders = await repository.GetBySellerIdAsync(request.SellerId, ct);

        var dtos = orders.Select(order => new OrderDto(
            order.Id,
            order.BuyerId,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.CompletedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id, i.Sku, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice)).ToList())).ToList();

        return Result<List<OrderDto>>.Success(dtos);
    }
}
