namespace ApiGateway.Contracts;

/// <summary>
/// Enriched cart DTO returned to Angular. Contains product details from the Catalog service.
/// </summary>
public sealed record CartDto(
    string BuyerId,
    List<CartItemDetailsDto> Items,
    decimal TotalPrice,
    int TotalItems);

/// <summary>
/// Cart item with product metadata (title, image) enriched from the Catalog service.
/// </summary>
public sealed record CartItemDetailsDto(
    Guid ProductId,
    string Title,
    string? ImageUrl,
    int Quantity,
    decimal Price,
    decimal LineTotal,
    Guid StoreId);

/// <summary>
/// Raw cart response from cart-api (mirrors Cart.Application.Dtos.CartResponse).
/// </summary>
internal sealed record RawCartResponse(
    string BuyerId,
    List<RawCartItemResponse> Items,
    decimal TotalPrice,
    int TotalItems,
    DateTime UpdatedAt);

internal sealed record RawCartItemResponse(
    Guid ProductId,
    Guid StoreId,
    int Quantity,
    decimal Price,
    decimal LineTotal);

/// <summary>
/// Product summary from catalog-api (mirrors ProductListDto).
/// </summary>
internal sealed record ProductSummary(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string Sku,
    string CategoryName,
    string Status,
    string? ImageUrl,
    Guid StoreId,
    DateTime CreatedAt);
