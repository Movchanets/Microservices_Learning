using BuildingBlocks.SharedContracts.Commands.Payment;
using BuildingBlocks.SharedContracts.Events.Payment;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Commands.RefundPayment;
using Payment.Domain.Aggregates;
using Payment.Domain.Enumerations;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Consumes RefundPaymentIntegrationCommand from the Ordering saga's compensation path.
/// Creates a Refund entity, reverses the original payment via the gateway,
/// and publishes PaymentRefundedEvent.
/// </summary>
public sealed class RefundPaymentConsumer(
    ISender sender,
    IPaymentTransactionRepository transactionRepo,
    ILogger<RefundPaymentConsumer> logger) : IConsumer<RefundPaymentIntegrationCommand>
{
    public async Task Consume(ConsumeContext<RefundPaymentIntegrationCommand> context)
    {
        var cmd = context.Message;
        logger.LogInformation(
            "Processing refund for Order {OrderId}, Amount {Amount}, Reason {Reason}",
            cmd.OrderId, cmd.Amount, cmd.Reason);

        // Look up the transaction by OrderId
        var transaction = await transactionRepo.GetByOrderIdAsync(cmd.OrderId, context.CancellationToken);

        if (transaction is null)
        {
            logger.LogWarning("No payment transaction found for Order {OrderId} — skipping refund", cmd.OrderId);
            return;
        }

        if (transaction.Status != PaymentStatus.Completed)
        {
            logger.LogWarning(
                "Payment transaction {TransactionId} for Order {OrderId} has status {Status} — skipping refund",
                transaction.Id, cmd.OrderId, transaction.Status);
            return;
        }

        var refundCmd = new RefundPaymentCommand(transaction.Id, cmd.Reason, cmd.Amount);
        var result = await sender.Send(refundCmd, context.CancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Refund {RefundId} processed for Order {OrderId}, Transaction {TransactionId}",
                result.Value, cmd.OrderId, transaction.Id);
        }
        else
        {
            logger.LogError(
                "Refund failed for Order {OrderId}: {Error} — throwing for MassTransit retry",
                cmd.OrderId, result.Error);

            // Throw so MassTransit retries with redelivery policy.
            // Leaving the system in "cancelled but not refunded" state is unacceptable.
            throw new InvalidOperationException(
                $"Refund failed for Order {cmd.OrderId}: {result.Error}");
        }
    }
}
