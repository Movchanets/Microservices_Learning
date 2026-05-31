using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using MediatR;

namespace Cart.Application.Commands;

public record AddCartItemCommand(Guid? BuyerId, Guid? CartId, Guid ProductId, Guid SkuId, string SkuCode, int Quantity) : IRequest<Result<CartResponse>>;

public sealed class AddCartItemCommandHandler(
    ICartRepository repository,
    IProductPriceRepository priceRepository) : IRequestHandler<AddCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var productPrice = await priceRepository.GetBySkuIdAsync(request.SkuId, cancellationToken);
        if (productPrice is null)
            return Result<CartResponse>.Failure($"SKU '{request.SkuId}' not found");

        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, request.CartId, cancellationToken);
        cart.AddItem(request.ProductId, request.SkuId, request.SkuCode, request.Quantity, productPrice.StoreId, productPrice.Price);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
