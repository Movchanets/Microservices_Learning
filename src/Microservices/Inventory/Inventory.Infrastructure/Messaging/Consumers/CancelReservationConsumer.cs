using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Events.Inventory;
using Inventory.Application.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class CancelReservationConsumer(
    ISender sender,
    ILogger<CancelReservationConsumer> logger) : IConsumer<CancelReservationCommand>
{
    public async Task Consume(ConsumeContext<CancelReservationCommand> context)
    {
        var cmd = context.Message;
        logger.LogInformation("Processing CancelReservationCommand for Order {OrderId}", cmd.OrderId);

        var result = await sender.Send(new ReleaseStockCommand(cmd.OrderId, cmd.Items));

        if (result.IsSuccess)
        {
            await context.Publish(new InventoryReleasedEvent(cmd.CorrelationId, cmd.OrderId));
        }
    }
}