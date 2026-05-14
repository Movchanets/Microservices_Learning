using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.Domain.Events;

public sealed record StockReservedDomainEvent(
    Guid InventoryItemId, 
    string Sku, 
    int Quantity) : IDomainEvent;