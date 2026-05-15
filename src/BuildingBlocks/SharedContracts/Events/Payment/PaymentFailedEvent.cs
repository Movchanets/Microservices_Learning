namespace BuildingBlocks.SharedContracts.Events.Payment;

public record PaymentFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string FailureReason);
