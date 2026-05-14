using BuildingBlocks.SharedContracts.Abstractions;

namespace Inventory.Domain.Events;

public sealed record StockReleasedDomainEvent(
    Guid InventoryItemId, 
    string Sku, 
    int Quantity) : IDomainEvent;