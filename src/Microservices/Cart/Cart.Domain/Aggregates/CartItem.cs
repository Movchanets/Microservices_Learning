using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

/// <summary>
/// Represents a single line item in a buyer's shopping cart.
/// Each item references a specific SKU from a product and tracks quantity and price at time of addition.
/// Cart items are scoped to a buyer session and expire after a configurable TTL.
/// </summary>
public sealed class CartItem : Entity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid SkuId { get; private set; }
    public string SkuCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public Guid StoreId { get; private set; }

    private CartItem() { } // EF Core

    internal CartItem(Guid cartId, Guid productId, Guid skuId, string skuCode, int quantity, decimal price, Guid storeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId is required", nameof(storeId));

        CartId = cartId;
        ProductId = productId;
        SkuId = skuId;
        SkuCode = skuCode.Trim().ToUpperInvariant();
        Quantity = quantity;
        Price = price;
        StoreId = storeId;
    }

    /// <summary>
    /// Matches this item against a (ProductId, SkuId) composite identity.
    /// </summary>
    public bool MatchesProduct(Guid productId, Guid skuId)
    {
        return ProductId == productId && SkuId == skuId;
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }

    public void SetQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity = quantity;
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));
        Price = price;
    }
}
