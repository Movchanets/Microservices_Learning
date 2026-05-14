namespace Search.API.Models;

public sealed class ProductSearchDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? ImageUrl { get; set; }
    public Guid SellerId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record SearchResult<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int Page,
    int PageSize,
    Dictionary<string, List<FacetValue>>? Facets = null);

public sealed record FacetValue(string Key, long Count);
