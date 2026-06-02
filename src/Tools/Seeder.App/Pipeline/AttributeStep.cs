using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 4: Create attribute definitions from two sources:
///   1. Static definitions in categories.json (for base categories)
///   2. Product VariantAxes (auto-detected from product data)
/// </summary>
public class AttributeStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;

    public AttributeStep(HttpClient client, ILogger logger, string dataDirectory)
    {
        _client = client;
        _logger = logger;
        _dataDirectory = dataDirectory;
    }

    public async Task ExecuteAsync(
        string adminToken,
        List<ProductModel> products,
        List<CategoryDto> categories,
        CancellationToken ct)
    {
        var categorySeeder = new CategorySeeder(_client, _logger);

        // ── Source 1: Static definitions from categories.json ────
        await CreateStaticDefinitionsAsync(categorySeeder, adminToken, categories, ct);

        // ── Source 2: Auto-detected from product VariantAxes ────
        await CreateVariantAxisDefinitionsAsync(
            categorySeeder, adminToken, products, categories, ct);
    }

    /// <summary>
    /// Creates attribute definitions declared in categories.json.
    /// Each category can specify its own AttributeDefinitions array.
    /// </summary>
    private async Task CreateStaticDefinitionsAsync(
        CategorySeeder categorySeeder,
        string adminToken,
        List<CategoryDto> categories,
        CancellationToken ct)
    {
        var categoriesToSeed = await SeedDataLoader.LoadJsonAsync<List<CategoryModel>>(
            _dataDirectory, "categories.json");
        var totalCount = 0;

        foreach (var categoryModel in categoriesToSeed)
        {
            if (categoryModel.AttributeDefinitions == null
                || categoryModel.AttributeDefinitions.Count == 0)
                continue;

            var categoryId = CategoryResolver.FindBest(categoryModel.Name, categories);
            if (categoryId == null)
            {
                _logger.LogWarning(
                    "Could not find category '{Name}' for static attribute definitions.",
                    categoryModel.Name);
                continue;
            }

            foreach (var attr in categoryModel.AttributeDefinitions)
            {
                await categorySeeder.EnsureAttributeDefinitionAsync(
                    categoryId.Value, attr, adminToken, ct);
                totalCount++;
            }

            _logger.LogInformation(
                "Seeded {Count} static attribute definitions on category '{Name}'",
                categoryModel.AttributeDefinitions.Count, categoryModel.Name);
        }

        if (totalCount > 0)
            _logger.LogInformation("Total static attribute definitions: {Count}", totalCount);
    }

    /// <summary>
    /// Auto-detects variant axes from product VariantAxes dictionaries
    /// and creates Select-type attribute definitions on the matching categories.
    /// </summary>
    private async Task CreateVariantAxisDefinitionsAsync(
        CategorySeeder categorySeeder,
        string adminToken,
        List<ProductModel> products,
        List<CategoryDto> categories,
        CancellationToken ct)
    {
        // Collect unique (categoryName → axisKey → axisValues)
        var axesToCreate = new Dictionary<string, Dictionary<string, List<string>>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            if (product.Variants == null || product.Variants.Count == 0)
                continue;

            if (!axesToCreate.ContainsKey(product.CategoryName))
                axesToCreate[product.CategoryName] = new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var variant in product.Variants)
            {
                if (variant.Attributes == null) continue;
                foreach (var (key, value) in variant.Attributes)
                {
                    if (!axesToCreate[product.CategoryName].ContainsKey(key))
                        axesToCreate[product.CategoryName][key] = new List<string>();

                    if (!axesToCreate[product.CategoryName][key]
                            .Contains(value, StringComparer.OrdinalIgnoreCase))
                        axesToCreate[product.CategoryName][key].Add(value);
                }
            }
        }

        if (axesToCreate.Count == 0)
        {
            _logger.LogInformation("No variant axes found in product data — skipping auto-detection.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} categories with variant axes from product data: {Categories}",
            axesToCreate.Count, string.Join(", ", axesToCreate.Keys));

        foreach (var (categoryName, axes) in axesToCreate)
        {
            var categoryId = CategoryResolver.FindBest(categoryName, categories);

            // Fallback: try breadcrumb segments
            if (categoryId == null && categoryName.Contains('>'))
            {
                foreach (var segment in categoryName.Split('>').Select(s => s.Trim()))
                {
                    var segMatch = categories.FirstOrDefault(c =>
                        c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
                    if (segMatch != null)
                    {
                        categoryId = segMatch.Id;
                        break;
                    }
                }
            }

            if (categoryId == null)
            {
                _logger.LogWarning(
                    "Could not find category '{CategoryName}' for attribute definitions.",
                    categoryName);
                continue;
            }

            var sortOrder = 1;
            foreach (var (key, values) in axes)
            {
                var displayName = key[..1].ToUpperInvariant() + key[1..];
                var attr = new AttributeDefinitionModel(
                    Key: key,
                    DisplayName: displayName,
                    Target: 1,         // Sku
                    ValueType: 2,      // Select
                    IsFilterable: true,
                    IsRequired: true,
                    SortOrder: sortOrder++,
                    AllowedValues: values,
                    IsVariantAxis: true);

                await categorySeeder.EnsureAttributeDefinitionAsync(
                    categoryId.Value, attr, adminToken, ct);
            }

            _logger.LogInformation(
                "Seeded {Count} variant-axis attribute definitions on category '{CategoryName}'",
                axes.Count, categoryName);
        }
    }
}
