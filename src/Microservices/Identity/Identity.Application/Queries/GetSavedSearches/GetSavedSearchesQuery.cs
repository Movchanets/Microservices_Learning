using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetSavedSearches;

public sealed record GetSavedSearchesQuery(Guid UserId) : IRequest<List<SavedSearchDto>>;
