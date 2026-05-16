namespace Search.API.Models;

public sealed class ProductSearchDocument
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string Sku { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string? ImageUrl { get; init; }
    public Guid StoreId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record SearchResult<T>(
    List<T> Items,
    long TotalCount,
    int Page,
    int PageSize,
    Dictionary<string, List<FacetValue>>? Facets = null)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public sealed record FacetValue(string Key, long Count);
