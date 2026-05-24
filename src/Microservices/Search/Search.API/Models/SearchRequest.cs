namespace Search.API.Models;

public sealed record SearchRequest(
    string? Query,
    Guid? CategoryId,
    decimal? PriceMin,
    decimal? PriceMax,
    List<string>? Tags,
    string? Brand,
    double? MinRating,
    bool? InStock,
    int Page = 1,
    int PageSize = 20);
