using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.Domain.Events;

public sealed record StockReservedDomainEvent(
    Guid InventoryItemId,
    Guid StoreId,
    string Sku,
    int Quantity) : IDomainEvent;
