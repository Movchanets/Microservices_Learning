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
        var skus = request.Items.Select(i => i.Sku).ToList();
        var items = await repository.GetBySkusAsync(skus, cancellationToken);

        foreach (var requestedItem in request.Items)
        {
            var inventoryItem = items.FirstOrDefault(i => i.Sku == requestedItem.Sku);
            if (inventoryItem == null)
            {
                return Result<bool>.Failure($"Inventory item not found for SKU {requestedItem.Sku}");
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