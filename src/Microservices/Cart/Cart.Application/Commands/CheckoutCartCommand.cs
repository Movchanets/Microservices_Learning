using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Cart;
using Cart.Domain.Aggregates;
using MassTransit;
using MediatR;

namespace Cart.Application.Commands;

public record CheckoutResponseDto(Guid CorrelationId);
public record CheckoutCartCommand(
    string BuyerId,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null) : IRequest<Result<CheckoutResponseDto>>;

public sealed class CheckoutCartCommandHandler(
    ICartRepository repository,
    IPublishEndpoint publishEndpoint) : IRequestHandler<CheckoutCartCommand, Result<CheckoutResponseDto>>
{
    public async Task<Result<CheckoutResponseDto>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetCartAsync(request.BuyerId, cancellationToken);

        if (cart.Items.Count == 0)
        {
            return Result<CheckoutResponseDto>.Failure("Cart is empty.");
        }

        var correlationId = Guid.NewGuid();
        var itemsContract = cart.Items.Select(i => new OrderItemContract(i.Sku, i.Quantity, i.Price, i.SellerId)).ToList();

        var orderSubmittedEvent = new OrderSubmittedEvent(
            correlationId,
            request.BuyerId,
            itemsContract,
            DateTime.UtcNow,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country
        );

        await publishEndpoint.Publish(orderSubmittedEvent, cancellationToken);

        await repository.DeleteCartAsync(request.BuyerId, cancellationToken);

        return Result<CheckoutResponseDto>.Success(new CheckoutResponseDto(correlationId));
    }
}