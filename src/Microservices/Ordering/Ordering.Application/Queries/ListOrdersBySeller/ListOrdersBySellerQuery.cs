using BuildingBlocks.Infrastructure.Models;
using Ordering.Application.DTOs;
using MediatR;

namespace Ordering.Application.Queries.ListOrdersBySeller;

public sealed record ListOrdersBySellerQuery(string SellerId) : IRequest<Result<List<OrderDto>>>;
