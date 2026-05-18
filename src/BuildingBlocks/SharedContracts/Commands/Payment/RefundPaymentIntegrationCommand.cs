namespace BuildingBlocks.SharedContracts.Commands.Payment;

public record RefundPaymentIntegrationCommand(
    Guid CorrelationId,
    Guid OrderId,
    Guid TransactionId,
    decimal Amount,
    string Reason);
