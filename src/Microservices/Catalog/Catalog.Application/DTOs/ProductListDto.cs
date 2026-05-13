namespace Catalog.Application.DTOs;

public sealed record ProductListDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string Sku,
    string CategoryName,
    string Status,
    string? ImageUrl,
    DateTime CreatedAt);
