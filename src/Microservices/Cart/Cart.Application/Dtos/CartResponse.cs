using Cart.Domain.Aggregates;

namespace Cart.Application.Dtos;

public sealed record CartResponse(
    Guid? BuyerId,
    Guid CartId,
    List<CartItemResponse> Items,
    decimal TotalPrice,
    int TotalItems,
    DateTime UpdatedAt);

public sealed record CartItemResponse(
    Guid ProductId,
    Guid StoreId,
    int Quantity,
    decimal Price,
    decimal LineTotal);

public static class CartMapper
{
    public static CartResponse ToResponse(ShoppingCart cart) => new(
        cart.BuyerId,
        cart.Id,
        cart.Items.Select(i => new CartItemResponse(
            i.ProductId, i.StoreId, i.Quantity, i.Price, i.Price * i.Quantity)).ToList(),
        cart.Items.Sum(i => i.Price * i.Quantity),
        cart.Items.Sum(i => i.Quantity),
        cart.UpdatedAt);
}
