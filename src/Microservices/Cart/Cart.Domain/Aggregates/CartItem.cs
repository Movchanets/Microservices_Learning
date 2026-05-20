using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Aggregates;

public sealed class CartItem : Entity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public Guid StoreId { get; private set; }

    private CartItem() { } // EF Core

    internal CartItem(Guid cartId, Guid productId, int quantity, decimal price, Guid storeId)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("CartId cannot be empty", nameof(cartId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId is required", nameof(storeId));

        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
        Price = price;
        StoreId = storeId;
    }

    /// <summary>
    /// Matches this item against a ProductId.
    /// </summary>
    internal bool MatchesProduct(Guid productId)
    {
        return ProductId == productId;
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
