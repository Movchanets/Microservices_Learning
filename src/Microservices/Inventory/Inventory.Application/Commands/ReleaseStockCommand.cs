using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.Infrastructure.Models;
using MediatR;

namespace Inventory.Application.Commands;

public record ReleaseStockCommand(Guid OrderId, List<OrderItemContract> Items) : IRequest<Result<bool>>;