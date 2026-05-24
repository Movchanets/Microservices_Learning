using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.Infrastructure.Models;
using Inventory.Domain.Aggregates;
using MediatR;

namespace Inventory.Application.Commands;

public sealed class ReleaseStockCommandHandler(
    IInventoryItemRepository repository,
    IUnitOfWork uow) : IRequestHandler<ReleaseStockCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
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
            if (inventoryItem != null)
            {
                inventoryItem.Release(requestedItem.Quantity);
                repository.Update(inventoryItem);
            }
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
