using BuildingBlocks.SharedContracts.Commands.Inventory;
using BuildingBlocks.SharedContracts.Events.Inventory;
using Inventory.Application.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Consumers;

public sealed class ReserveInventoryConsumer(
    ISender sender,
    ILogger<ReserveInventoryConsumer> logger) : IConsumer<ReserveInventoryCommand>
{
    public async Task Consume(ConsumeContext<ReserveInventoryCommand> context)
    {
        var cmd = context.Message;
        logger.LogInformation("Processing ReserveInventoryCommand for Order {OrderId}", cmd.OrderId);

        var result = await sender.Send(new ReserveStockCommand(cmd.OrderId, cmd.Items));

        if (result.IsSuccess)
        {
            await context.Publish(new InventoryReservedEvent(cmd.CorrelationId, cmd.OrderId));
        }
        else
        {
            await context.Publish(new InventoryReservationFailedEvent(cmd.CorrelationId, cmd.OrderId, result.Error ?? "Unknown error"));
        }
    }
}