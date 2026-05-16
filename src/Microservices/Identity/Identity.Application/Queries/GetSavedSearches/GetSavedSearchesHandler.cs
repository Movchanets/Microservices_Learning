using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using MediatR;

namespace Identity.Application.Queries.GetSavedSearches;

public sealed class GetSavedSearchesHandler(
    ISavedSearchRepository repository)
    : IRequestHandler<GetSavedSearchesQuery, List<SavedSearchDto>>
{
    public async Task<List<SavedSearchDto>> Handle(
        GetSavedSearchesQuery request,
        CancellationToken cancellationToken)
    {
        var searches = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return searches.Select(s => new SavedSearchDto(
            s.Id,
            s.Query,
            s.FiltersJson,
            s.PriceAlertEnabled,
            s.CreatedAt)).ToList();
    }
}
