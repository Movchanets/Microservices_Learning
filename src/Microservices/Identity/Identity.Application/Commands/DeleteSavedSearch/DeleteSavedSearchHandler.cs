using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Commands.DeleteSavedSearch;

public sealed class DeleteSavedSearchHandler(
    ISavedSearchRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSavedSearchCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteSavedSearchCommand request,
        CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(request.SearchId, request.UserId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
