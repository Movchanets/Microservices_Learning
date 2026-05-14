namespace BuildingBlocks.SharedContracts.Events.Inventory;

public record InventoryReleasedEvent(
    Guid CorrelationId,
    Guid OrderId);