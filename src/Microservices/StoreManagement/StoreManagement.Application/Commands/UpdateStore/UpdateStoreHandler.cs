using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using MediatR;
using StoreManagement.Application.DTOs;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Application.Commands.UpdateStore;

public sealed class UpdateStoreHandler(
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStoreCommand, Result<StoreDto>>
{
    public async Task<Result<StoreDto>> Handle(
        UpdateStoreCommand request,
        CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<StoreDto>.Failure("Store not found.", "NOT_FOUND");
        }

        store.UpdateDetails(request.Name, request.Description);

        storeRepository.Update(store);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StoreDto>.Success(new StoreDto(
            store.Id,
            store.SellerId,
            store.Name,
            store.Description,
            store.LogoUrl,
            store.VerificationStatus.ToString(),
            store.RejectionReason,
            store.CreatedAt,
            store.UpdatedAt,
            store.VerifiedAt));
    }
}
