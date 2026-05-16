using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Ordering.Application.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string BuyerId,
    List<CreateOrderItemDto> Items,
    string? ShippingAddressLine1,
    string? ShippingAddressLine2,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry) : IRequest<Result<Guid>>;

public sealed record CreateOrderItemDto(
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
