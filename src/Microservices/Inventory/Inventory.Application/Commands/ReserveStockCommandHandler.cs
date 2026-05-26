using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.Infrastructure.Models;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Exceptions;
using MediatR;

namespace Inventory.Application.Commands;

public sealed class ReserveStockCommandHandler(
    IInventoryItemRepository repository,
    IUnitOfWork uow) : IRequestHandler<ReserveStockCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var resolved = await InventoryItemResolver.ResolveAsync(request.Items, repository, cancellationToken);

        foreach (var (requestedItem, inventoryItem) in resolved)
        {
            if (inventoryItem is null)
                return Result<bool>.Failure($"Inventory item not found for SKU {requestedItem.SkuCode}");

            try
            {
                inventoryItem.Reserve(requestedItem.Quantity);
                repository.Update(inventoryItem);
            }
            catch (OutOfStockException ex) { return Result<bool>.Failure(ex.Message); }
            catch (InvalidOperationException ex) { return Result<bool>.Failure(ex.Message); }
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
