using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

/// <summary>
/// The Shopping Cart aggregate root. Represents a buyer's active cart session.
/// Uses Redis as the backing store (thin service — no EF Core for cart data).
/// Cart items are managed as a collection within this aggregate.
/// Optimistic concurrency via PostgreSQL xmin system column prevents lost updates.
/// </summary>
public sealed class ShoppingCart : AggregateRoot
{
    public Guid? BuyerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Row version used for optimistic concurrency. Mapped to PostgreSQL xmin system column.
    /// </summary>
    public uint Version { get; private set; }

    private const int MaxItems = 50;

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private ShoppingCart() { } // EF Core

    public ShoppingCart(Guid? buyerId)
    {
        BuyerId = buyerId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, Guid skuId, string skuCode, int quantity, Guid storeId, decimal price = 0m)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));
        if (skuId == Guid.Empty)
            throw new ArgumentException("SkuId is required", nameof(skuId));
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId is required", nameof(storeId));

        var existingItem = _items.FirstOrDefault(i => i.MatchesProduct(productId, skuId));
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            if (_items.Count >= MaxItems)
                throw new InvalidOperationException($"Cart cannot exceed {MaxItems} items");

            _items.Add(new CartItem(Id, productId, skuId, skuCode, quantity, price, storeId));
        }

        Touch();
    }

    public void UpdateQuantity(Guid productId, Guid skuId, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.MatchesProduct(productId, skuId));
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

    public void RemoveItem(Guid productId, Guid skuId)
    {
        var existingItem = _items.FirstOrDefault(i => i.MatchesProduct(productId, skuId));
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

    /// <summary>
    /// Claims this anonymous cart for an authenticated user.
    /// Sets the BuyerId on a cart that was created without one.
    /// </summary>
    public void Claim(Guid buyerId)
    {
        if (BuyerId.HasValue)
            throw new InvalidOperationException("Cart is already claimed by a buyer.");

        BuyerId = buyerId;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
