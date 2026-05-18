using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using MediatR;

namespace Cart.Application.Commands;

public record AddCartItemCommand(string BuyerId, string Sku, int Quantity, string? SellerId = null) : IRequest<Result<ShoppingCart>>;

public sealed class AddCartItemCommandHandler(
    ICartRepository repository,
    IProductPriceRepository priceRepository) : IRequestHandler<AddCartItemCommand, Result<ShoppingCart>>
{
    public async Task<Result<ShoppingCart>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var productPrice = await priceRepository.GetBySkuAsync(request.Sku, cancellationToken);
        if (productPrice is null)
            return Result<ShoppingCart>.Failure($"Product with SKU '{request.Sku}' not found");

        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, cancellationToken);
        cart.AddItem(request.Sku, request.Quantity, productPrice.Price, request.SellerId);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<ShoppingCart>.Success(cart);
    }
}
