using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Commands.Inventory;

public record ReserveInventoryCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemContract> Items);