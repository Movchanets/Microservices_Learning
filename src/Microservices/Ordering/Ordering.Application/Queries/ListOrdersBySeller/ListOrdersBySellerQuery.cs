using BuildingBlocks.Infrastructure.Models;
using MediatR;
using Ordering.Application.DTOs;

namespace Ordering.Application.Queries.ListOrdersBySeller;

public sealed record ListOrdersBySellerQuery(Guid StoreId) : IRequest<Result<List<OrderDto>>>;
