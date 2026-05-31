using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.SharedContracts.Events.Cart;
using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using MassTransit;
using MediatR;

namespace Cart.Application.Commands;

public record CheckoutResponseDto(Guid CorrelationId);
public record CheckoutCartCommand(
    Guid BuyerId,
    Guid? CartId = null,
    AddressRequest? Address = null) : IRequest<Result<CheckoutResponseDto>>;

public sealed class CheckoutCartCommandHandler(
    ICartRepository repository,
    IProductPriceRepository priceRepository,
    IPublishEndpoint publishEndpoint) : IRequestHandler<CheckoutCartCommand, Result<CheckoutResponseDto>>
{
    public async Task<Result<CheckoutResponseDto>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetCartAsync(request.BuyerId, request.CartId, cancellationToken);
        if (cart.Items.Count == 0)
            return Result<CheckoutResponseDto>.Failure("Cart is empty.");

        var correlationId = Guid.NewGuid();
        var itemsContract = await BuildOrderItemsAsync(cart, cancellationToken);

        await publishEndpoint.Publish(new OrderSubmittedEvent(
            correlationId, request.BuyerId.ToString(), itemsContract, DateTime.UtcNow,
            request.Address?.AddressLine1, request.Address?.AddressLine2,
            request.Address?.City, request.Address?.State,
            request.Address?.PostalCode, request.Address?.Country), cancellationToken);

        await repository.DeleteCartAsync(request.BuyerId, request.CartId, cancellationToken);
        return Result<CheckoutResponseDto>.Success(new CheckoutResponseDto(correlationId));
    }

    private async Task<List<OrderItemContract>> BuildOrderItemsAsync(ShoppingCart cart, CancellationToken ct)
    {
        var skuIds = cart.Items.Select(i => i.SkuId).ToList();
        var priceEntries = await priceRepository.GetBySkuIdsAsync(skuIds, ct);
        var nameLookup = priceEntries.ToDictionary(p => p.SkuId, p => p.Name);

        return [.. cart.Items.Select(i => new OrderItemContract(
            i.ProductId, i.SkuId, i.SkuCode,
            nameLookup.GetValueOrDefault(i.SkuId, i.SkuCode),
            i.Quantity, i.Price, i.StoreId))];
    }
}
