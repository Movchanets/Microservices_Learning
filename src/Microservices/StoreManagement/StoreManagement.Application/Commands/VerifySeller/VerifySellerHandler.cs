using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using MediatR;
using StoreManagement.Domain.Aggregates;

namespace StoreManagement.Application.Commands.VerifySeller;

public sealed class VerifySellerHandler(
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifySellerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        VerifySellerCommand request,
        CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Result<Guid>.Failure("Store not found.", "NOT_FOUND");
        }

        if (request.IsApproved)
        {
            store.Verify();
        }
        else
        {
            store.Reject(request.Reason ?? "Rejected by administrator");
        }

        storeRepository.Update(store);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(store.Id);
    }
}
