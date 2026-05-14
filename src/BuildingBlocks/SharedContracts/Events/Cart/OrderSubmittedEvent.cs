using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Events.Cart;

public record OrderSubmittedEvent(
    Guid CorrelationId,
    string BuyerId,
    List<OrderItemContract> Items,
    DateTime Timestamp);