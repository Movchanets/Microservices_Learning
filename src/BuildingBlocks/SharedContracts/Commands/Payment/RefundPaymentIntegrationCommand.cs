namespace BuildingBlocks.SharedContracts.Commands.Payment;

/// <summary>
/// Integration command sent by the Ordering Saga to the Payment Service
/// to refund a previously completed payment (compensation / rollback).
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order whose payment is being refunded.</param>
/// <param name="TransactionId">Original payment transaction identifier.</param>
/// <param name="Amount">Amount to refund.</param>
/// <param name="Reason">Human-readable reason for the refund.</param>
public record RefundPaymentIntegrationCommand(
    Guid CorrelationId,
    Guid OrderId,
    Guid TransactionId,
    decimal Amount,
    string Reason);
