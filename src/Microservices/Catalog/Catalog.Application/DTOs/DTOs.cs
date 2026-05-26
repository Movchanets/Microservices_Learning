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
    List<string> AllowedValues);
