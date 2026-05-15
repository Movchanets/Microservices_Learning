using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using MediatR;
using StoreManagement.Application.DTOs;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Application.Commands.CreateStore;

public sealed class CreateStoreHandler(
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStoreCommand, Result<StoreDto>>
{
    public async Task<Result<StoreDto>> Handle(
        CreateStoreCommand request,
        CancellationToken cancellationToken)
    {
        if (await storeRepository.ExistsBySellerIdAsync(request.SellerId, cancellationToken))
        {
            return Result<StoreDto>.Failure(
                $"Store already exists for seller '{request.SellerId}'.", "STORE_DUPLICATE");
        }

        var store = Store.Create(request.SellerId, request.Name, request.Description);

        storeRepository.Add(store);
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
