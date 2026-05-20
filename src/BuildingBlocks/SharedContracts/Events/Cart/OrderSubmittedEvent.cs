using BuildingBlocks.SharedContracts.Dtos;

namespace BuildingBlocks.SharedContracts.Events.Cart;

/// <summary>
/// Published by Cart.API when a buyer completes checkout.
/// Consumed by Ordering Service to create the order aggregate and start the ordering saga.
/// </summary>
/// <param name="CorrelationId">Saga correlation ID that traces the entire order flow.</param>
/// <param name="BuyerId">Identity of the buyer (from Identity service).</param>
/// <param name="Items">Cart items at time of submission.</param>
/// <param name="Timestamp">When the order was submitted.</param>
/// <param name="ShippingAddressLine1">Optional shipping address line 1.</param>
/// <param name="ShippingAddressLine2">Optional shipping address line 2.</param>
/// <param name="ShippingCity">Optional shipping city.</param>
/// <param name="ShippingState">Optional shipping state/province.</param>
/// <param name="ShippingPostalCode">Optional shipping postal/zip code.</param>
/// <param name="ShippingCountry">Optional shipping country.</param>
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