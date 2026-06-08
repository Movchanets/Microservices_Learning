namespace Catalog.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    Guid CategoryId,
    string CategoryName,
    string Status,
    string? ImageUrl,
    string? Brand,
    Guid StoreId,
    List<string> Tags,
    List<SkuDto> Skus,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ProductListDto(
    Guid Id,
    string Name,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Currency,
    int SkuCount,
    Guid? DefaultSkuId,
    string? DefaultSkuCode,
    string CategoryName,
    string Status,
    string? ImageUrl,
    Guid StoreId,
    DateTime CreatedAt);

public sealed record SkuDto(
    Guid Id,
    string SkuCode,
    decimal Price,
    string Currency,
    string Status,
    string? ImageUrl,
    Dictionary<string, string> TypedAttributes,
    Dictionary<string, string> FlexibleAttributes,
    DateTime CreatedAt);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string Slug,
    int SortOrder,
    bool IsActive);

public sealed record AttributeDefinitionDto(
    Guid Id,
    string Key,
    string DisplayName,
    string Target,
    string ValueType,
    bool IsFilterable,
    bool IsRequired,
    int SortOrder,
    List<string> AllowedValues,
    bool IsInherited = false);

/// <summary>
/// Result of a bulk SKU creation operation.
/// </summary>
public sealed record BulkAddSkuResultDto(
    int CreatedCount,
    int TotalCombinations,
    List<SkuDto> CreatedSkus,
    List<string>? Errors = null);

/// <summary>
/// Variant matrix for a product — shows all possible attribute combinations
/// and which ones have active SKUs. Used by the frontend variant picker.
/// </summary>
public sealed record VariantMatrixDto(
    Guid ProductId,
    string ProductName,
    List<VariantAxisDto> Axes,
    List<VariantOptionDto> Options);

/// <summary>
/// A single variant axis (e.g., "Color" with values ["Black","White","Blue"]).
/// </summary>
public sealed record VariantAxisDto(
    string Key,
    string DisplayName,
    List<string> Values);

/// <summary>
/// A single combination of variant values and its availability.
/// </summary>
public sealed record VariantOptionDto(
    Dictionary<string, string> Combination,
    Guid? SkuId,
    string? SkuCode,
    decimal? Price,
    string? Currency,
    string? ImageUrl,
    bool IsAvailable);
