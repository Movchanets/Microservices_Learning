namespace BuildingBlocks.SharedContracts.Commands.Payment;

public record ProcessPaymentCommand(
    Guid CorrelationId,
    Guid OrderId,
    decimal Amount,
    string BuyerId);
