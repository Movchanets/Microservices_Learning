using Catalog.Domain.Entities;

namespace Catalog.Application.DTOs;

public record CategoryTreeDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string Slug,
    int SortOrder,
    bool IsActive,
    List<CategoryTreeDto> Children)
{
    public static CategoryTreeDto FromEntity(Category category) =>
        new(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.Slug,
            category.SortOrder,
            category.IsActive,
            []);
}
