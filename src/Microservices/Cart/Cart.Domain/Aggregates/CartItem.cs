using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class CartItem : Entity
{
    public string CartId { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public string? SellerId { get; private set; }

    // Navigation property back to parent
    public ShoppingCart Cart { get; private set; } = null!;

    private CartItem() { } // EF Core

    internal CartItem(string cartId, string sku, int quantity, decimal price = 0m, string? sellerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cartId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        CartId = cartId;
        Sku = sku;
        Quantity = quantity;
        Price = price;
        SellerId = sellerId;
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