using BuildingBlocks.Infrastructure.Models;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Queries;

public record GetCartQuery(string BuyerId) : IRequest<Result<ShoppingCart>>;

public sealed class GetCartQueryHandler(ICartRepository repository) : IRequestHandler<GetCartQuery, Result<ShoppingCart>>
{
    public async Task<Result<ShoppingCart>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetCartAsync(request.BuyerId, cancellationToken);
        return Result<ShoppingCart>.Success(cart);
    }
}