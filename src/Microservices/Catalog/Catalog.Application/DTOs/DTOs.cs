namespace Catalog.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    Guid CategoryId,
    string CategoryName,
    string Status,
    string? ImageUrl,
    Guid StoreId,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ProductListDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string Sku,
    string CategoryName,
    string Status,
    string? ImageUrl,
    Guid StoreId,
    DateTime CreatedAt);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string Slug,
    int SortOrder,
    bool IsActive);
