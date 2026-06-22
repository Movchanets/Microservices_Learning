using Seeder.App.Models;

namespace Seeder.App.Pipeline;

/// <summary>
/// Resolves category names to category IDs using multiple matching strategies.
/// Used by CategoryStep, AttributeStep, and ProductStep.
/// </summary>
public static class CategoryResolver
{
    /// <summary>
    /// Finds the best matching category for a given name.
    ///
    /// Priority:
    ///   1. Exact mapping from category-mapping.json
    ///   2. Exact name match (case-insensitive)
    ///   3. Breadcrumb path — last segment first (most specific)
    ///   4. Partial match (contains)
    ///   5. First breadcrumb segment fallback
    /// </summary>
    public static Guid? FindBest(
        string categoryName,
        List<CategoryDto> categories,
        Dictionary<string, string>? mapping = null)
    {
        // Priority 1: Exact mapping
        if (mapping != null)
        {
            if (TryResolveFromMapping(categoryName, categories, mapping, out var mappedId))
                return mappedId;

            if (categoryName.Contains('>'))
            {
                foreach (var segment in categoryName.Split('>').Select(s => s.Trim()))
                {
                    if (TryResolveFromMapping(segment, categories, mapping, out var segId))
                        return segId;
                }
            }
        }

        // Priority 2: Exact name match
        var exact = categories.FirstOrDefault(c =>
            c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact.Id;

        // Priority 3: Breadcrumb path — last segment first
        if (categoryName.Contains('>'))
        {
            var segments = categoryName.Split('>').Select(s => s.Trim()).Reverse();
            foreach (var segment in segments)
            {
                var match = categories.FirstOrDefault(c =>
                    c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.Id;
            }
        }

        // Priority 4: Partial match
        var partial = categories.FirstOrDefault(c =>
            c.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase) ||
            categoryName.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
        if (partial != null) return partial.Id;

        // Priority 5: First breadcrumb segment fallback
        if (categoryName.Contains('>'))
        {
            var first = categoryName.Split('>')[0].Trim();
            var firstMatch = categories.FirstOrDefault(c =>
                c.Name.Equals(first, StringComparison.OrdinalIgnoreCase));
            if (firstMatch != null) return firstMatch.Id;
        }

        return null;
    }

    /// <summary>
    /// Finds the parent category from a breadcrumb path.
    /// Looks for the segment before the given category name.
    /// </summary>
    public static Guid? FindParent(
        string catName,
        List<string> breadcrumbPaths,
        List<CategoryDto> resultCategories)
    {
        foreach (var path in breadcrumbPaths)
        {
            var segments = path.Split('>').Select(s => s.Trim()).ToList();
            var idx = segments.FindIndex(s =>
                s.Equals(catName, StringComparison.OrdinalIgnoreCase));
            if (idx > 0)
            {
                var parentName = segments[idx - 1].Trim();
                if (parentName.Length > 2 && parentName != "Інтернет-магазин Rozetka")
                {
                    var parentCat = resultCategories.FirstOrDefault(c =>
                        c.Name.Equals(parentName, StringComparison.OrdinalIgnoreCase));
                    if (parentCat != null)
                        return parentCat.Id;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Tries to resolve a category name through the mapping dictionary.
    /// </summary>
    public static bool TryResolveFromMapping(
        string name,
        List<CategoryDto> categories,
        Dictionary<string, string> mapping,
        out Guid? categoryId)
    {
        categoryId = null;
        if (!mapping.TryGetValue(name, out var mappedName))
            return false;

        var match = categories.FirstOrDefault(c =>
            c.Name.Equals(mappedName, StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        categoryId = match.Id;
        return true;
    }
}
