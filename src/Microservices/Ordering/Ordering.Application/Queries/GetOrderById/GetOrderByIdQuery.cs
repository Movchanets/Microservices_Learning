using BuildingBlocks.Infrastructure.Models;
using Ordering.Application.DTOs;
using MediatR;

namespace Ordering.Application.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto>>;
