using BuildingBlocks.Infrastructure.Models;
using MediatR;
using StoreManagement.Application.DTOs;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;

namespace StoreManagement.Application.Queries.ListStores;

public sealed class ListStoresHandler(
    IStoreRepository storeRepository)
    : IRequestHandler<ListStoresQuery, Result<IReadOnlyList<StoreListDto>>>
{
    public async Task<Result<IReadOnlyList<StoreListDto>>> Handle(
        ListStoresQuery request,
        CancellationToken cancellationToken)
    {
        var stores = await storeRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<VerificationStatus>(request.Status, true, out var status))
        {
            stores = stores.Where(s => s.VerificationStatus == status).ToList();
        }

        var dtos = stores.Select(s => new StoreListDto(
            s.Id,
            s.Name,
            s.SellerId,
            s.LogoUrl,
            s.VerificationStatus.ToString(),
            s.CreatedAt)).ToList();

        return Result<IReadOnlyList<StoreListDto>>.Success(dtos);
    }
}
