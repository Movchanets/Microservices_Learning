namespace Search.API.Models;

public sealed class ProductSearchDocument
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public string Currency { get; init; } = "USD";
    public int SkuCount { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string? ImageUrl { get; init; }
    public Guid StoreId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Richer faceting
    public string? Brand { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = [];

    /// <summary>
    /// Variant axes and their available values for faceted search.
    /// Example: { "color": ["Black","White","Blue"], "storage": ["128GB","256GB"] }
    /// Populated from SKU TypedAttributes for variant-axis keys.
    /// </summary>
    public Dictionary<string, List<string>> VariantAxes { get; init; } = [];

    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    public bool InStock { get; init; }
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

/// <summary>
/// Groups all metadata fields for a product search document update.
/// Replaces the 11-parameter UpdateProductMetadataAsync signature.
/// </summary>
public sealed record UpdateProductMetadataRequest(
    Guid ProductId,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    List<string> Tags,
    string? ImageUrl,
    Guid StoreId,
    bool IsActive,
    DateTime UpdatedAt,
    string? Brand,
    Dictionary<string, string>? Attributes);
