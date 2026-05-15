using Ordering.Domain.Enumerations;

namespace Ordering.Application.DTOs;

public sealed record OrderDto(
    Guid Id,
    string BuyerId,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);
