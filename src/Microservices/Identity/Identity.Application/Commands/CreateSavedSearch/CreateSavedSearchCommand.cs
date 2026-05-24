using BuildingBlocks.Infrastructure.Models;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands.CreateSavedSearch;

public sealed record CreateSavedSearchCommand(
    Guid UserId,
    string Query,
    string FiltersJson,
    bool PriceAlertEnabled = false) : IRequest<Result<SavedSearchDto>>;
