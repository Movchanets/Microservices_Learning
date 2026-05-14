using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Commands.Inventory;

public record CancelReservationCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemContract> Items);