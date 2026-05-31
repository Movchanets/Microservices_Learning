using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record UpdateCartItemCommand(Guid? BuyerId, Guid? CartId, Guid ProductId, Guid SkuId, int Quantity) : IRequest<Result<CartResponse>>;

public sealed class UpdateCartItemCommandHandler(ICartRepository repository) : IRequestHandler<UpdateCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, request.CartId, cancellationToken);
        cart.UpdateQuantity(request.ProductId, request.SkuId, request.Quantity);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
