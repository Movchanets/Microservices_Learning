using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Dtos;
using BuildingBlocks.Infrastructure.Models;
using Inventory.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Commands;

public sealed class ReleaseStockCommandHandler(
    IInventoryItemRepository repository,
    IUnitOfWork uow,
    ILogger<ReleaseStockCommandHandler> logger) : IRequestHandler<ReleaseStockCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var resolved = await InventoryItemResolver.ResolveAsync(request.Items, repository, cancellationToken);

        foreach (var (requestedItem, inventoryItem) in resolved)
        {
            if (inventoryItem is null)
            {
                logger.LogWarning(
                    "Inventory item not found for SKU {SkuCode} (SkuId={SkuId}) — reserved stock may be leaked",
                    requestedItem.SkuCode, requestedItem.SkuId);
                continue;
            }

            inventoryItem.Release(requestedItem.Quantity);
            repository.Update(inventoryItem);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
