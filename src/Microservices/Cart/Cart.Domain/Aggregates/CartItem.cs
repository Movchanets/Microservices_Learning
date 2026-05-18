using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class CartItem : Entity
{
    public Guid CartId { get; private set; } 
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public string? ShopId { get; private set; }

    private CartItem() { } // EF Core

    internal CartItem(Guid cartId, string sku, int quantity, decimal price = 0m, string? shopId = null)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("CartId cannot be empty", nameof(cartId));
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        CartId = cartId;
        Sku = sku;
        Quantity = quantity;
        Price = price;
        ShopId = shopId;
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }

    public void SetQuantity(int quantity)
    {
        Quantity = quantity;
    }

    public void SetPrice(decimal price)
    {
        Price = price;
    }
}