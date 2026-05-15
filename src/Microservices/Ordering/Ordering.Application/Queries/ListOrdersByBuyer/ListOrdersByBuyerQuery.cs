using BuildingBlocks.Infrastructure.Models;
using Ordering.Application.DTOs;
using MediatR;

namespace Ordering.Application.Queries.ListOrdersByBuyer;

public sealed record ListOrdersByBuyerQuery(string BuyerId) : IRequest<Result<List<OrderDto>>>;
