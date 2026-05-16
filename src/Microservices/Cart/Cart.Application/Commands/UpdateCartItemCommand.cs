using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record UpdateCartItemCommand(string BuyerId, string Sku, int Quantity) : IRequest<Result<ShoppingCart>>;

public sealed class UpdateCartItemCommandHandler(ICartRepository repository) : IRequestHandler<UpdateCartItemCommand, Result<ShoppingCart>>
{
    public async Task<Result<ShoppingCart>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetCartAsync(request.BuyerId, cancellationToken);
        cart.UpdateQuantity(request.Sku, request.Quantity);

        await repository.UpdateCartAsync(cart, cancellationToken);
        return Result<ShoppingCart>.Success(cart);
    }
}
