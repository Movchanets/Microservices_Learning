using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Commands.Payment;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Aggregates;
using Ordering.Domain.Enumerations;

namespace Ordering.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentProcessingConsumer(
    IOrderRepository repository,
    IUnitOfWork uow,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderPaymentProcessingConsumer> logger) : IConsumer<ProcessPaymentCommand>
{
    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var cmd = context.Message;
        var order = await OrderConsumerHelpers.LoadOrderOrThrowAsync(repository, cmd.OrderId, context.CancellationToken);

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Faulted)
        {
            logger.LogInformation(
                "Skipping PaymentProcessing projection for Order {OrderId} because status is {Status}",
                order.Id, order.Status);
            return;
        }

        if (order.Status == OrderStatus.Submitted)
        {
            logger.LogWarning(
                "Order {OrderId} is still Submitted when ProcessPayment arrived — fast-forwarding to InventoryReserved",
                order.Id);
        }

        await OrderConsumerHelpers.FastForwardAndPublishAsync(
            repository, uow, publishEndpoint, order, OrderStatus.PaymentProcessing, null, context.CancellationToken);
    }
}
