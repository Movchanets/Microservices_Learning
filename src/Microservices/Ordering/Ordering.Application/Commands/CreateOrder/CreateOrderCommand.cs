using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Ordering.Application.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string BuyerId,
    List<CreateOrderItemDto> Items) : IRequest<Result<Guid>>;

public sealed record CreateOrderItemDto(
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
