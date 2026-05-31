using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

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
        if (cartId == Guid.Empty)
            throw new ArgumentException("CartId cannot be empty", nameof(cartId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));
        if (skuId == Guid.Empty)
            throw new ArgumentException("SkuId cannot be empty", nameof(skuId));
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
