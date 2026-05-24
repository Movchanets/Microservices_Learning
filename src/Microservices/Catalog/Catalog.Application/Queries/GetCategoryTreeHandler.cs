using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Queries;

public class GetCategoryTreeHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoryTreeQuery, List<CategoryTreeDto>>
{
    public async Task<List<CategoryTreeDto>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var activeCategoriesFromDb = await categoryRepository.GetActiveAsync(cancellationToken);

        var activeCategories = activeCategoriesFromDb
            .Select(CategoryTreeDto.FromEntity)
            .ToList();

        var categoryMap = activeCategories.ToDictionary(c => c.Id);
        var rootCategories = new List<CategoryTreeDto>();

        foreach (var category in activeCategories)
        {
            if (category.ParentCategoryId.HasValue && categoryMap.TryGetValue(category.ParentCategoryId.Value, out var parent))
            {
                parent.Children.Add(category);
            }
            else
            {
                rootCategories.Add(category);
            }
        }

        // Sort children and roots recursively
        SortTree(rootCategories);

        return rootCategories;
    }

    private static void SortTree(List<CategoryTreeDto> nodes)
    {
        nodes.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortTree(node.Children);
            }
        }
    }
}
