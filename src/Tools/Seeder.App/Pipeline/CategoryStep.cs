using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 3: Create categories from categories.json + Rozetka breadcrumb paths.
/// Two-pass: parents first, then children (resolves ParentCategoryName → ParentCategoryId).
/// Returns the full list of categories (existing + created).
/// </summary>
public class CategoryStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;

    public CategoryStep(HttpClient client, ILogger logger, string dataDirectory)
    {
        _client = client;
        _logger = logger;
        _dataDirectory = dataDirectory;
    }

    public async Task<List<CategoryDto>> ExecuteAsync(
        string adminToken, List<ProductModel> products, CancellationToken ct)
    {
        var categorySeeder = new CategorySeeder(_client, _logger);
        var categoriesToSeed = await SeedDataLoader.LoadJsonAsync<List<CategoryModel>>(
            _dataDirectory, "categories.json");
        var existingCategories = await categorySeeder.GetExistingCategoriesAsync(ct);
        var resultCategories = new List<CategoryDto>(existingCategories);

        // ── Pass 1: Create parent categories (no ParentCategoryName) ──
        foreach (var category in categoriesToSeed.Where(c => string.IsNullOrEmpty(c.ParentCategoryName)))
        {
            var created = await categorySeeder.EnsureCategoryExistsAsync(
                category, adminToken, existingCategories, ct);
            if (created != null && !resultCategories.Any(c => c.Name == created.Name))
                resultCategories.Add(created);
        }

        // ── Pass 2: Create child categories (has ParentCategoryName) ──
        foreach (var category in categoriesToSeed.Where(c => !string.IsNullOrEmpty(c.ParentCategoryName)))
        {
            // Resolve parent name → parent ID
            var parentCat = resultCategories.FirstOrDefault(c =>
                c.Name.Equals(category.ParentCategoryName, StringComparison.OrdinalIgnoreCase));

            var resolved = category;
            if (parentCat != null)
            {
                resolved = category with { ParentCategoryId = parentCat.Id };
            }
            else
            {
                _logger.LogWarning(
                    "Parent category '{ParentName}' not found for '{ChildName}'. Creating as top-level.",
                    category.ParentCategoryName, category.Name);
            }

            var created = await categorySeeder.EnsureCategoryExistsAsync(
                resolved, adminToken, existingCategories, ct);
            if (created != null && !resultCategories.Any(c => c.Name == created.Name))
                resultCategories.Add(created);
        }

        // ── Pass 3: Create nested categories from Rozetka breadcrumbs ──
        var breadcrumbPaths = products
            .Where(p => p.CategoryName.Contains('>'))
            .Select(p => p.CategoryName)
            .Distinct()
            .ToList();

        var segmentOrder = new List<string>();
        foreach (var path in breadcrumbPaths)
        {
            var segments = path.Split('>').Select(s => s.Trim())
                .Where(s => s.Length > 2 && s != "Інтернет-магазин Rozetka")
                .ToList();
            foreach (var seg in segments)
            {
                if (!segmentOrder.Contains(seg, StringComparer.OrdinalIgnoreCase))
                    segmentOrder.Add(seg);
            }
        }

        foreach (var catName in segmentOrder)
        {
            if (resultCategories.Any(c =>
                c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase)))
                continue;

            Guid? parentId = CategoryResolver.FindParent(catName, breadcrumbPaths, resultCategories);

            var created = await categorySeeder.EnsureCategoryExistsAsync(
                new CategoryModel(catName, $"Rozetka: {catName}", parentId),
                adminToken, existingCategories, ct);
            if (created != null)
                resultCategories.Add(created);
        }

        return resultCategories;
    }
}
