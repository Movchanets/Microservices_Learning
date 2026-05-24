namespace BuildingBlocks.SharedContracts.Events.Ordering;

/// <summary>
/// Published by the Ordering Saga when all steps (inventory, payment) succeed.
/// Consumed by Notification Service to send order confirmation to the buyer.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The completed order.</param>
/// <param name="BuyerId">Identity of the buyer.</param>
public record OrderCompletedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string BuyerId);
