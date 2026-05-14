using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record CartItemDto(string Sku, int Quantity);
public record UpdateCartCommand(string BuyerId, List<CartItemDto> Items) : IRequest<Result<ShoppingCart>>;

public sealed class UpdateCartCommandHandler(ICartRepository repository) : IRequestHandler<UpdateCartCommand, Result<ShoppingCart>>
{
    public async Task<Result<ShoppingCart>> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetCartAsync(request.BuyerId, cancellationToken);
        cart.Clear();
        foreach (var item in request.Items)
        {
            cart.AddItem(item.Sku, item.Quantity);
        }

        await repository.UpdateCartAsync(cart, cancellationToken);
        return Result<ShoppingCart>.Success(cart);
    }
}