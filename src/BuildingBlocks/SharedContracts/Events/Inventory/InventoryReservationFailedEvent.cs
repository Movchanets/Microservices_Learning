namespace BuildingBlocks.SharedContracts.Events.Inventory;

/// <summary>
/// Published by Inventory Service when stock reservation fails
/// (e.g. insufficient stock, product not found).
/// Consumed by the Ordering Saga to trigger order cancellation / compensation.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order that failed reservation.</param>
/// <param name="Reason">Human-readable reason for the failure.</param>
public record InventoryReservationFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason);