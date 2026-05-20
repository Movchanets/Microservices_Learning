using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Aggregates.Entities;

public sealed class OrderItem : Entity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Guid StoreId { get; private set; }

    private OrderItem() { }

    internal OrderItem(Guid productId, string productName, decimal unitPrice, int quantity, Guid storeId)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId is required", nameof(productId));
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (storeId == Guid.Empty) throw new ArgumentException("StoreId is required", nameof(storeId));

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        StoreId = storeId;
    }

    public decimal TotalPrice => UnitPrice * Quantity;
}
