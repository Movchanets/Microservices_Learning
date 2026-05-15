using BuildingBlocks.SharedContracts.Abstractions;

namespace Ordering.Domain.Aggregates.Entities;

public sealed class OrderItem : Entity
{
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    internal OrderItem(string sku, string productName, decimal unitPrice, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        if (unitPrice <= 0) throw new ArgumentException("Unit price must be positive", nameof(unitPrice));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Sku = sku;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public decimal TotalPrice => UnitPrice * Quantity;
}
