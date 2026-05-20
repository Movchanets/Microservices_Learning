namespace BuildingBlocks.SharedContracts.Commands.Payment;

/// <summary>
/// Integration command sent by the Ordering Saga to the Payment Service
/// to charge the buyer for an order.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order being paid for.</param>
/// <param name="Amount">Total payment amount.</param>
/// <param name="BuyerId">Identity of the buyer being charged.</param>
public record ProcessPaymentCommand(
    Guid CorrelationId,
    Guid OrderId,
    decimal Amount,
    string BuyerId);
