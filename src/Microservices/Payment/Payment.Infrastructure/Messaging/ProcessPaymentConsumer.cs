using BuildingBlocks.SharedContracts.Commands.Payment;
using BuildingBlocks.SharedContracts.Events.Payment;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Commands.ProcessPayment;
using Payment.Infrastructure.External;

namespace Payment.Infrastructure.Messaging;

public sealed class ProcessPaymentConsumer(
    ISender sender,
    IPaymentGateway gateway,
    ILogger<ProcessPaymentConsumer> logger) : IConsumer<ProcessPaymentCommand>
{
    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var cmd = context.Message;
        logger.LogInformation("Processing payment for Order {OrderId}, Amount {Amount}", cmd.OrderId, cmd.Amount);

        var gatewayResult = await gateway.ProcessPaymentAsync(
            cmd.OrderId, cmd.Amount, cmd.BuyerId, context.CancellationToken);

        if (gatewayResult.IsSuccess)
        {
            await sender.Send(new ProcessPaymentInternalCommand(
                cmd.CorrelationId, cmd.OrderId, cmd.Amount, cmd.BuyerId), context.CancellationToken);

            await context.Publish(new PaymentCompletedEvent(
                cmd.CorrelationId, cmd.OrderId, gatewayResult.TransactionId!));

            logger.LogInformation("Payment completed for Order {OrderId}, TransactionId {TransactionId}",
                cmd.OrderId, gatewayResult.TransactionId);
        }
        else
        {
            await context.Publish(new PaymentFailedEvent(
                cmd.CorrelationId, cmd.OrderId, gatewayResult.FailureReason!));

            logger.LogWarning("Payment failed for Order {OrderId}: {Reason}",
                cmd.OrderId, gatewayResult.FailureReason);
        }
    }
}
