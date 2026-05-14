using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class CartItem : Entity
{
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }

    private CartItem() { } // EF Core

    internal CartItem(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    internal void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }

    internal void SetQuantity(int quantity)
    {
        Quantity = quantity;
    }
}