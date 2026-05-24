using BuildingBlocks.Infrastructure.Models;
using Cart.Application.Dtos;
using Cart.Domain.Aggregates;
using MediatR;

namespace Cart.Application.Queries;

public record GetCartQuery(Guid? BuyerId, Guid? CartId) : IRequest<Result<CartResponse>>;

public sealed class GetCartQueryHandler(ICartRepository repository) : IRequestHandler<GetCartQuery, Result<CartResponse>>
{
    public async Task<Result<CartResponse>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await repository.GetCartAsync(request.BuyerId, request.CartId, cancellationToken);
        return Result<CartResponse>.Success(CartMapper.ToResponse(cart));
    }
}
