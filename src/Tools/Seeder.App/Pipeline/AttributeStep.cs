using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 4: Create attribute definitions from catalog.json
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
        CatalogDataModel catalogData,
        Dictionary<string, Guid> categoryMapping,
        CancellationToken ct)
    {
        var categorySeeder = new CategorySeeder(_client, _logger);
        int totalCount = 0;

        var allBrands = catalogData.BaseProducts
            .Select(p => p.Brand)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct()
            .ToList();

        foreach (var attr in catalogData.AttributeDefinitions)
        {
            if (!categoryMapping.TryGetValue(attr.CategoryId, out var categoryGuid))
            {
                _logger.LogWarning("Category {CategoryId} not found for attribute {Name}", attr.CategoryId, attr.Name);
                continue;
            }

            var displayName = attr.Name.Length > 0 ? char.ToUpper(attr.Name[0]) + attr.Name.Substring(1) : attr.Name;
            
            var allowedValues = attr.PossibleValues?.ToList() ?? new List<string>();
            if (attr.Name.Equals("brand", StringComparison.OrdinalIgnoreCase))
            {
                allowedValues.AddRange(allBrands!);
                allowedValues = allowedValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            var model = new AttributeDefinitionModel(
                Key: attr.Name,
                DisplayName: displayName,
                Target: 1, // Sku
                ValueType: 2, // Select
                IsFilterable: true,
                IsRequired: false,
                SortOrder: totalCount,
                AllowedValues: allowedValues,
                IsVariantAxis: true
            );

            await categorySeeder.EnsureAttributeDefinitionAsync(
                categoryGuid, model, adminToken, ct);
            totalCount++;
        }

        // Ensure "brand" definition exists for all categories even if not in catalogData.AttributeDefinitions
        foreach (var categoryGuid in categoryMapping.Values)
        {
            var brandModel = new AttributeDefinitionModel(
                Key: "brand",
                DisplayName: "Brand",
                Target: 1, // Sku
                ValueType: 2, // Select
                IsFilterable: true,
                IsRequired: false,
                SortOrder: totalCount,
                AllowedValues: allBrands,
                IsVariantAxis: true
            );
            await categorySeeder.EnsureAttributeDefinitionAsync(
                categoryGuid, brandModel, adminToken, ct);
        }

        _logger.LogInformation("Total attribute definitions seeded: {Count}", totalCount);
    }
}
