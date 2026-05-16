using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class CartItem : Entity
{
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }

    private CartItem() { } // EF Core

    internal CartItem(string sku, int quantity, decimal price = 0m)
    {
        Sku = sku;
        Quantity = quantity;
        Price = price;
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