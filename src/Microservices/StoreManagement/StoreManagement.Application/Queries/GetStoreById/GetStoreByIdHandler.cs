using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Application.Queries.GetStoreById;

public sealed class GetStoreByIdHandler(
    IStoreRepository storeRepository)
    : IRequestHandler<GetStoreByIdQuery, Result<StoreDto>>
{
    public async Task<Result<StoreDto>> Handle(
        GetStoreByIdQuery request,
        CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<StoreDto>.Failure("Store not found.", "NOT_FOUND");
        }

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
