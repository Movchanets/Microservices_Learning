using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record CartItemDto(string Sku, int Quantity, decimal Price, string? ShopId = null);
public record UpdateCartCommand(string BuyerId, List<CartItemDto> Items) : IRequest<Result<CartResponse>>;

public sealed class UpdateCartCommandHandler(ICartRepository repository) : IRequestHandler<UpdateCartCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, cancellationToken);
        cart.Clear();
        foreach (var item in request.Items)
            cart.AddItem(item.Sku, item.Quantity, item.Price, item.ShopId);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
