using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record RemoveCartItemCommand(string BuyerId, string Sku) : IRequest<Result<ShoppingCart>>;

public sealed class RemoveCartItemCommandHandler(ICartRepository repository) : IRequestHandler<RemoveCartItemCommand, Result<ShoppingCart>>
{
    public async Task<Result<ShoppingCart>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, cancellationToken);
        cart.RemoveItem(request.Sku);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<ShoppingCart>.Success(cart);
    }
}
