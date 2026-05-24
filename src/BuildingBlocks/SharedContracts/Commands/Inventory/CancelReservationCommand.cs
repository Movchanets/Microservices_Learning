using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Commands.Inventory;

/// <summary>
/// Integration command sent by the Ordering Saga to the Inventory Service
/// to release previously reserved stock (compensation / rollback).
/// </summary>
/// <param name="CorrelationId">Saga correlation ID.</param>
/// <param name="OrderId">The order whose reservation should be cancelled.</param>
/// <param name="Items">List of products and quantities to release.</param>
public record CancelReservationCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemContract> Items);