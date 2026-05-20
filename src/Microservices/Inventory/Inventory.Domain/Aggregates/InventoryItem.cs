using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Aggregates;

public sealed class InventoryItem : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int AvailableQuantity { get; private set; }
    public byte[] Version { get; private set; } = []; // For optimistic concurrency

    private InventoryItem() { }

    public static InventoryItem Create(string sku, int initialQuantity, Guid storeId, Guid productId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative", nameof(initialQuantity));
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId is required", nameof(storeId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));

        return new InventoryItem
        {
            ProductId = productId,
            StoreId = storeId,
            Sku = sku.Trim().ToUpperInvariant(),
            AvailableQuantity = initialQuantity,
            Version = []
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to add must be positive", nameof(quantity));

        AvailableQuantity += quantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to reserve must be positive", nameof(quantity));

        if (AvailableQuantity < quantity)
            throw new OutOfStockException(Sku, quantity, AvailableQuantity);

        AvailableQuantity -= quantity;

        AddDomainEvent(new StockReservedDomainEvent(Id, StoreId, Sku, quantity));
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to release must be positive", nameof(quantity));

        AvailableQuantity += quantity;

        AddDomainEvent(new StockReleasedDomainEvent(Id, StoreId, Sku, quantity));
    }

    /// <summary>
    /// Corrects the ProductId when seed data created the item with a placeholder.
    /// Called by ProductCreatedConsumer when the real ProductId becomes available.
    /// </summary>
    public void UpdateProductId(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));
        ProductId = productId;
    }
}