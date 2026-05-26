using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record CartItemDto(Guid ProductId, Guid SkuId, string SkuCode, int Quantity, decimal Price, Guid StoreId);
public record UpdateCartCommand(Guid? BuyerId, Guid? CartId, List<CartItemDto> Items) : IRequest<Result<CartResponse>>;

public sealed class UpdateCartCommandHandler(ICartRepository repository) : IRequestHandler<UpdateCartCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, request.CartId, cancellationToken);
        cart.Clear();
        foreach (var item in request.Items)
            cart.AddItem(item.ProductId, item.SkuId, item.SkuCode, item.Quantity, item.StoreId, item.Price);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
