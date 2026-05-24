using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.Domain.Events;

public sealed record StockReleasedDomainEvent(
    Guid InventoryItemId,
    Guid StoreId,
    string Sku,
    int Quantity) : IDomainEvent;
