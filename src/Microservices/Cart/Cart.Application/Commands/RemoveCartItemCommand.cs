using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record RemoveCartItemCommand(Guid? BuyerId, Guid? CartId, Guid ProductId, Guid SkuId) : IRequest<Result<CartResponse>>;

public sealed class RemoveCartItemCommandHandler(ICartRepository repository) : IRequestHandler<RemoveCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, request.CartId, cancellationToken);
        cart.RemoveItem(request.ProductId, request.SkuId);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
