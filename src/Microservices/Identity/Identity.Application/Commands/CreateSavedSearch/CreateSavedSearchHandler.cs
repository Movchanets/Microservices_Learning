using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Commands.CreateSavedSearch;

public sealed class CreateSavedSearchHandler(
    ISavedSearchRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSavedSearchCommand, Result<SavedSearchDto>>
{
    public async Task<Result<SavedSearchDto>> Handle(
        CreateSavedSearchCommand request,
        CancellationToken cancellationToken)
    {
        var search = SavedSearch.Create(
            request.UserId,
            request.Query,
            request.FiltersJson,
            request.PriceAlertEnabled);

        repository.Add(search);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SavedSearchDto>.Success(new SavedSearchDto(
            search.Id,
            search.Query,
            search.FiltersJson,
            search.PriceAlertEnabled,
            search.CreatedAt));
    }
}
