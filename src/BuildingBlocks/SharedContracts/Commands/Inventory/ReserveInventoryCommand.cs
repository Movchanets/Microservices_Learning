using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Commands.Inventory;

/// <summary>
/// Integration command sent by the Ordering Saga to the Inventory Service
/// to reserve stock for the items in an order.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID that traces the entire order flow.</param>
/// <param name="OrderId">The order being fulfilled.</param>
/// <param name="Items">List of products and quantities to reserve.</param>
public record ReserveInventoryCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemContract> Items);