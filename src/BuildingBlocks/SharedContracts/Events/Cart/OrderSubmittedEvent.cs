using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Events.Cart;

public record OrderSubmittedEvent(
    Guid CorrelationId,
    string BuyerId,
    List<OrderItemContract> Items,
    DateTime Timestamp,
    string? ShippingAddressLine1 = null,
    string? ShippingAddressLine2 = null,
    string? ShippingCity = null,
    string? ShippingState = null,
    string? ShippingPostalCode = null,
    string? ShippingCountry = null);