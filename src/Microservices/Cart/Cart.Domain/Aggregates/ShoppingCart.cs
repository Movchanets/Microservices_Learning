using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class ShoppingCart : AggregateRoot
{
    public string BuyerId { get; private set; } = string.Empty;

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private ShoppingCart() { } // EF Core

    public ShoppingCart(string buyerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerId);
        BuyerId = buyerId;
    }

    public void AddItem(string sku, int quantity, decimal price = 0m, string? sellerId = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            _items.Add(new CartItem(sku, quantity, price, sellerId));
        }
    }

    public void UpdateQuantity(string sku, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem != null)
        {
            if (quantity <= 0)
            {
                _items.Remove(existingItem);
            }
            else
            {
                existingItem.SetQuantity(quantity);
            }
        }
    }

    public void RemoveItem(string sku)
    {
        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem != null)
        {
            _items.Remove(existingItem);
        }
    }

    public void Clear()
    {
        _items.Clear();
    }
}