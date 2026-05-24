using BuildingBlocks.Infrastructure.Models;
using MediatR;
using Ordering.Domain.Aggregates;

namespace Ordering.Application.Queries.HasPurchased;

public sealed class HasPurchasedHandler(IOrderRepository repository)
    : IRequestHandler<HasPurchasedQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(HasPurchasedQuery request, CancellationToken cancellationToken)
    {
        var orders = await repository.GetByBuyerIdAsync(request.BuyerId, cancellationToken);
        var hasPurchased = orders.Any(o => o.Items.Any(i => i.ProductId == request.ProductId));
        return Result<bool>.Success(hasPurchased);
    }
}
