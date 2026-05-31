using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Aggregates;

public sealed class InventoryItem : AggregateRoot
{
    /// <summary>
    /// The SKU ID from the Catalog service. Primary stock tracking key.
    /// </summary>
    public Guid SkuId { get; private set; }

    public Guid ProductId { get; private set; }
    public Guid StoreId { get; private set; }
    public string SkuCode { get; private set; } = string.Empty;
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public bool IsDeactivated { get; private set; }
    public byte[] Version { get; private set; } = []; // For optimistic concurrency

    private InventoryItem() { }

    /// <summary>
    /// Creates a new inventory item for a SKU. Called by SkuCreatedConsumer.
    /// </summary>
    public static InventoryItem Create(Guid skuId, Guid productId, string skuCode, int initialQuantity, Guid storeId)
    {
        if (skuId == Guid.Empty)
            throw new ArgumentException("SkuId is required", nameof(skuId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative", nameof(initialQuantity));
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId is required", nameof(storeId));

        return new InventoryItem
        {
            SkuId = skuId,
            ProductId = productId,
            StoreId = storeId,
            SkuCode = skuCode.Trim().ToUpperInvariant(),
            AvailableQuantity = initialQuantity,
            ReservedQuantity = 0,
            IsDeactivated = false,
            Version = []
        };
    }

    public void AddStock(int quantity)
    {
        if (IsDeactivated)
            throw new InvalidOperationException($"Cannot add stock to deactivated SKU {SkuCode}");
        if (quantity <= 0)
            throw new ArgumentException("Quantity to add must be positive", nameof(quantity));

        AvailableQuantity += quantity;
    }

    public void Reserve(int quantity)
    {
        if (IsDeactivated)
            throw new InvalidOperationException($"Cannot reserve stock for deactivated SKU {SkuCode}");
        if (quantity <= 0)
            throw new ArgumentException("Quantity to reserve must be positive", nameof(quantity));

        if (AvailableQuantity < quantity)
            throw new OutOfStockException(SkuCode, quantity, AvailableQuantity);

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;

        AddDomainEvent(new StockReservedDomainEvent(Id, StoreId, SkuCode, quantity));
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to release must be positive", nameof(quantity));

        if (quantity > ReservedQuantity)
            throw new InvalidOperationException(
                $"Cannot release {quantity} units — only {ReservedQuantity} reserved for SKU {SkuCode}");

        AvailableQuantity += quantity;
        ReservedQuantity -= quantity;

        AddDomainEvent(new StockReleasedDomainEvent(Id, StoreId, SkuCode, quantity));
    }

    /// <summary>
    /// Deactivates this inventory item when the corresponding SKU is deleted in Catalog.
    /// Zeros out AvailableQuantity and sets IsDeactivated flag.
    /// ReservedQuantity is left intact so in-flight orders can complete.
    /// Release() is still allowed on deactivated items to complete in-flight reservations.
    /// </summary>
    public void Deactivate()
    {
        if (IsDeactivated) return; // Idempotent

        IsDeactivated = true;
        AvailableQuantity = 0;
    }
}
