namespace BuildingBlocks.SharedContracts.Events.Inventory;

public record InventoryReservationFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason);