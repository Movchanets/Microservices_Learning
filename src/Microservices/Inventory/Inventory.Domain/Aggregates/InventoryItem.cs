using BuildingBlocks.SharedContracts.Abstractions;
using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Aggregates;

public sealed class InventoryItem : AggregateRoot
{
    public string Sku { get; private set; } = string.Empty;
    public int AvailableQuantity { get; private set; }
    public byte[] Version { get; private set; } = []; // For optimistic concurrency

    private InventoryItem() { }

    public static InventoryItem Create(string sku, int initialQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative", nameof(initialQuantity));

        return new InventoryItem
        {
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

        AddDomainEvent(new StockReservedDomainEvent(Id, Sku, quantity));
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to release must be positive", nameof(quantity));

        AvailableQuantity += quantity;

        AddDomainEvent(new StockReleasedDomainEvent(Id, Sku, quantity));
    }
}