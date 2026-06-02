using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Aggregates.Entities;

/// <summary>
/// A line item within an Order. Captures the SKU, quantity, and unit price at time of purchase.
/// Child entity of the Order aggregate — cannot exist independently.
/// </summary>
public sealed class OrderItem : Entity
{
    public Guid ProductId { get; private set; }
    public Guid SkuId { get; private set; }
    public string SkuCode { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Guid StoreId { get; private set; }

    private OrderItem() { }

    internal OrderItem(Guid productId, Guid skuId, string skuCode, string productName, decimal unitPrice, int quantity, Guid storeId)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required", nameof(productId));
        if (skuId == Guid.Empty) throw new ArgumentException("SkuId is required", nameof(skuId));
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (storeId == Guid.Empty) throw new ArgumentException("StoreId is required", nameof(storeId));

        ProductId = productId;
        SkuId = skuId;
        SkuCode = skuCode.Trim().ToUpperInvariant();
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        StoreId = storeId;
    }

    public decimal TotalPrice => UnitPrice * Quantity;
}
