namespace BuildingBlocks.SharedContracts.Events.Inventory;

public record InventoryReservedEvent(
    Guid CorrelationId,
    Guid OrderId);