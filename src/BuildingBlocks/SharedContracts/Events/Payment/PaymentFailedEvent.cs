namespace BuildingBlocks.SharedContracts.Events.Payment;

/// <summary>
/// Published by Payment Service when a payment attempt fails
/// (e.g. card declined, gateway error).
/// Consumed by the Ordering Saga to trigger full compensation
/// (release inventory, cancel order).
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order that failed payment.</param>
/// <param name="FailureReason">Human-readable reason for the failure.</param>
public record PaymentFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string FailureReason);
