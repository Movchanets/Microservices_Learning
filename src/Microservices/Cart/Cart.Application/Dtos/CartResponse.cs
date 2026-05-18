using Cart.Domain.Aggregates;

namespace Cart.Application.Dtos;

public sealed record CartResponse(
    string BuyerId,
    List<CartItemResponse> Items,
    decimal TotalPrice,
    int TotalItems,
    DateTime UpdatedAt);

public sealed record CartItemResponse(
    string Sku,
    int Quantity,
    decimal Price,
    decimal LineTotal,
    string? ShopId);

public static class CartMapper
{
    public static CartResponse ToResponse(ShoppingCart cart) => new(
        cart.BuyerId,
        cart.Items.Select(i => new CartItemResponse(
            i.Sku, i.Quantity, i.Price, i.Price * i.Quantity, i.ShopId)).ToList(),
        cart.Items.Sum(i => i.Price * i.Quantity),
        cart.Items.Sum(i => i.Quantity),
        cart.UpdatedAt);
}
