using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Application.Queries.GetStoreBySellerId;

public sealed class GetStoreBySellerIdHandler(
    IStoreRepository storeRepository)
    : IRequestHandler<GetStoreBySellerIdQuery, Result<StoreDto>>
{
    public async Task<Result<StoreDto>> Handle(
        GetStoreBySellerIdQuery request,
        CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetBySellerIdAsync(request.SellerId, cancellationToken);
        if (store is null)
        {
            return Result<StoreDto>.Failure("Store not found for seller.", "NOT_FOUND");
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
