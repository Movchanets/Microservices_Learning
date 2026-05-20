namespace ApiGateway.Contracts;

/// <summary>
/// Enriched order DTO returned to Angular. Contains product details from the Catalog service.
/// Status is returned as a string (e.g. "Submitted", "Completed") to match the frontend OrderStatus type.
/// </summary>
public sealed record OrderBffDto(
    Guid Id,
    string BuyerId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<OrderItemBffDto> Items);

/// <summary>
/// Order item with product metadata (name, image) enriched from the Catalog service.
/// </summary>
public sealed record OrderItemBffDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

/// <summary>
/// Raw order response from order-api.
/// </summary>
internal sealed record RawOrderDto(
    Guid Id,
    string BuyerId,
    int Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<RawOrderItemDto> Items);

internal sealed record RawOrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);
