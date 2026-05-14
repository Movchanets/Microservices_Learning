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
        var skus = request.Items.Select(i => i.Sku).ToList();
        var items = await repository.GetBySkusAsync(skus, cancellationToken);

        foreach (var requestedItem in request.Items)
        {
            var inventoryItem = items.FirstOrDefault(i => i.Sku == requestedItem.Sku);
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