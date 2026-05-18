using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class ShoppingCart : AggregateRoot
{
    public string BuyerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Row version used for optimistic concurrency. Mapped to PostgreSQL xmin system column.
    /// Automatically managed by the database — never set manually.
    /// </summary>
    public uint Version { get; private set; }

    private const int MaxItems = 50;

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private ShoppingCart() { } // EF Core

    public ShoppingCart(string buyerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerId);
        BuyerId = buyerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(string sku, int quantity, decimal price = 0m,  string? shopId = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            if (_items.Count >= MaxItems)
                throw new InvalidOperationException($"Cart cannot exceed {MaxItems} items");

            _items.Add(new CartItem(Id,sku, quantity, price, shopId));
        }

        Touch();
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

            Touch();
        }
    }

    public void RemoveItem(string sku)
    {
        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem != null)
        {
            _items.Remove(existingItem);
            Touch();
        }
    }

    public void Clear()
    {
        _items.Clear();
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}