namespace BuildingBlocks.SharedContracts.Events.Inventory;

/// <summary>
/// Published by Inventory Service after releasing previously reserved stock
/// (due to order cancellation or saga compensation).
/// Consumed by the Ordering Saga to confirm compensation completed.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order whose inventory reservation was released.</param>
public record InventoryReleasedEvent(
    Guid CorrelationId,
    Guid OrderId);