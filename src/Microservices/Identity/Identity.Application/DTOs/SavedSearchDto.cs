namespace Identity.Application.DTOs;

public sealed record SavedSearchDto(
    Guid Id,
    string Query,
    string FiltersJson,
    bool PriceAlertEnabled,
    DateTime CreatedAt);
