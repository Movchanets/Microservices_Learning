namespace BuildingBlocks.SharedContracts.Events.Inventory;

/// <summary>
/// Published by Inventory Service after successfully reserving stock for an order.
/// Consumed by the Ordering Saga to advance to the payment step.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order whose inventory was reserved.</param>
public record InventoryReservedEvent(
    Guid CorrelationId,
    Guid OrderId);