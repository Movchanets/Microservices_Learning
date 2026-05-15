namespace BuildingBlocks.SharedContracts.Events.Payment;

public record PaymentCompletedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string TransactionId);
