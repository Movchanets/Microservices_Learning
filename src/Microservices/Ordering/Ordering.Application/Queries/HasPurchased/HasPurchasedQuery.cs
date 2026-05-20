using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Ordering.Application.Queries.HasPurchased;

public sealed record HasPurchasedQuery(string BuyerId, Guid ProductId) : IRequest<Result<bool>>;
