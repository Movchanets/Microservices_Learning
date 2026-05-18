using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using MediatR;

namespace Cart.Application.Commands;

public record AddCartItemCommand(string BuyerId, string Sku, int Quantity, string? ShopId = null) : IRequest<Result<CartResponse>>;

public sealed class AddCartItemCommandHandler(
    ICartRepository repository,
    IProductPriceRepository priceRepository) : IRequestHandler<AddCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var productPrice = await priceRepository.GetBySkuAsync(request.Sku, cancellationToken);
        if (productPrice is null)
            return Result<CartResponse>.Failure($"Product with SKU '{request.Sku}' not found");

        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, cancellationToken);
        cart.AddItem(request.Sku, request.Quantity, productPrice.Price, request.ShopId);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
