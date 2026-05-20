using BuildingBlocks.SharedContracts.Abstractions;
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
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var items = new List<InventoryItem>();
        foreach (var pid in productIds)
        {
            var item = await repository.GetByProductIdAsync(pid, cancellationToken);
            if (item is not null) items.Add(item);
        }

        foreach (var requestedItem in request.Items)
        {
            var inventoryItem = items.FirstOrDefault(i => i.ProductId == requestedItem.ProductId);
            if (inventoryItem == null)
            {
                return Result<bool>.Failure($"Inventory item not found for product {requestedItem.ProductId}");
            }

            try
            {
                inventoryItem.Reserve(requestedItem.Quantity);
                repository.Update(inventoryItem);
            }
            catch (OutOfStockException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
