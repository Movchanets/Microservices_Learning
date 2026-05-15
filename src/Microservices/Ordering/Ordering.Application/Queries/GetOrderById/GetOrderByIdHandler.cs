using BuildingBlocks.Infrastructure.Models;
using Ordering.Application.DTOs;
using Ordering.Domain.Aggregates;
using MediatR;

namespace Ordering.Application.Queries.GetOrderById;

public sealed class GetOrderByIdHandler(
    IOrderRepository repository) : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Failure("Order not found");

        var dto = new OrderDto(
            order.Id,
            order.BuyerId,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.CompletedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id, i.Sku, i.ProductName, i.UnitPrice, i.Quantity, i.TotalPrice)).ToList());

        return Result<OrderDto>.Success(dto);
    }
}
