using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Commands;

public record RemoveCartItemCommand(string BuyerId, string Sku) : IRequest<Result<CartResponse>>;

public sealed class RemoveCartItemCommandHandler(ICartRepository repository) : IRequestHandler<RemoveCartItemCommand, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetOrCreateTrackedCartAsync(request.BuyerId, cancellationToken);
        cart.RemoveItem(request.Sku);

        await repository.SaveCartAsync(cart, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
