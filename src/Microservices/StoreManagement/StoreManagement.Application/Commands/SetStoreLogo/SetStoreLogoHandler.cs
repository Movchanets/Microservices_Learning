using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using MediatR;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Application.Commands.SetStoreLogo;

public sealed class SetStoreLogoHandler(
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetStoreLogoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        SetStoreLogoCommand request,
        CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<Guid>.Failure("Store not found.", "NOT_FOUND");
        }

        store.SetLogo(request.LogoUrl);

        storeRepository.Update(store);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(store.Id);
    }
}
