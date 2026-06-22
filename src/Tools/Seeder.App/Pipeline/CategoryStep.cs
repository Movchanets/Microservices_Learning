using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 3: Create categories from catalog.json
/// Returns a dictionary mapping ScrapedCategory.Id -> EF Category Guid
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

    public async Task<Dictionary<string, Guid>> ExecuteAsync(
        string adminToken, CatalogDataModel catalogData, CancellationToken ct)
    {
        var categorySeeder = new CategorySeeder(_client, _logger);
        var existingCategories = await categorySeeder.GetExistingCategoriesAsync(ct);
        var idMapping = new Dictionary<string, Guid>();

        foreach (var category in catalogData.Categories)
        {
            // We ignore ParentId for now as the tree is flat
            var catModel = new CategoryModel(category.Name, $"Rozetka: {category.Name}", null);
            var created = await categorySeeder.EnsureCategoryExistsAsync(
                catModel, adminToken, existingCategories, ct);

            if (created != null)
            {
                if (!existingCategories.Any(c => c.Id == created.Id))
                    existingCategories.Add(created);
                idMapping[category.Id] = created.Id;
            }
        }

        return idMapping;
    }
}
