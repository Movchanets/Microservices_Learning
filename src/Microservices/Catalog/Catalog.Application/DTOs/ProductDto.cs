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
    Guid SellerId,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
