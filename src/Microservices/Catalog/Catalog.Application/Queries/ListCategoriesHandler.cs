using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Queries;

public sealed class ListCategoriesHandler(
    ICategoryRepository categoryRepository)
    : IRequestHandler<ListCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        return categories.Select(c => new CategoryDto(
            c.Id, c.Name, c.Description, c.ParentCategoryId,
            c.Slug, c.SortOrder, c.IsActive)).ToList();
    }
}
