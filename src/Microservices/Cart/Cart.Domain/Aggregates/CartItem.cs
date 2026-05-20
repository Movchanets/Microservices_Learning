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

    /// <summary>
    /// Matches this item against a (Sku, ShopId) composite key.
    /// When ShopId is null on either side, falls back to SKU-only matching
    /// for backward compatibility with single-seller products.
    /// </summary>
    internal bool MatchesKey(string sku, string? shopId)
    {
        if (!string.Equals(Sku, sku, StringComparison.Ordinal))
            return false;

        // Both have ShopId → must match exactly
        if (ShopId is not null && shopId is not null)
            return string.Equals(ShopId, shopId, StringComparison.Ordinal);

        // At least one side has no ShopId → SKU-only match (backward compat)
        return true;
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
