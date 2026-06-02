using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.Application.Queries;

/// <summary>
/// Handles GetVariantMatrixQuery: retrieves a product and its category's AttributeDefinitions,
/// then builds a matrix showing which variant combinations (SKU × attribute values) exist
/// and which are still available for bulk creation.
/// </summary>
public sealed class GetVariantMatrixHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<GetVariantMatrixQuery, VariantMatrixDto?>
{
    public async Task<VariantMatrixDto?> Handle(
        GetVariantMatrixQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Load product with SKUs
        var product = await productRepository.GetWithSkusAsync(
            request.ProductId, cancellationToken);

        if (product is null)
            return null;

        // 2. Walk up category tree to collect variant-axis definitions
        //    (own definitions first, then inherited from parent chain)
        var variantDefs = new List<AttributeDefinition>();
        var visited = new HashSet<Guid>();
        var currentCategoryId = (Guid?)product.CategoryId;

        while (currentCategoryId.HasValue && !visited.Contains(currentCategoryId.Value))
        {
            var category = await categoryRepository.GetWithAttributeDefinitionsAsync(
                currentCategoryId.Value, cancellationToken);

            if (category is null) break;
            visited.Add(category.Id);

            foreach (var attr in category.AttributeDefinitions
                .Where(a => a.Target == AttributeTarget.Sku
                    && a.IsVariantAxis
                    && a.ValueType == AttributeType.Select)
                .OrderBy(a => a.SortOrder))
            {
                // Skip duplicates (child overrides parent with same key)
                if (variantDefs.Any(d => d.Key.Equals(attr.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;
                variantDefs.Add(attr);
            }

            currentCategoryId = category.ParentCategoryId;
        }

        if (variantDefs.Count == 0)
            return new VariantMatrixDto(product.Id, product.Name, [], []);

        // 4. Build axes — only values that appear in actual SKUs
        //    (prevents showing 100+ colors when the product only uses 3)
        var axes = variantDefs
            .Select(def => new VariantAxisDto(
                def.Key,
                def.DisplayName,
                GetDistinctValuesFromSkus(product.Skus, def.Key)))
            .Where(axis => axis.Values.Count > 0)
            .ToList();

        // 5. Generate Cartesian product of all axis values
        var combinations = GenerateCartesianProduct(axes);

        // 6. Match each combination to existing SKUs
        var activeSkus = product.Skus
            .Where(s => s.Status == SkuStatus.Active)
            .ToList();

        var axisKeys = axes.Select(a => a.Key).ToList();

        var options = combinations.Select(combo =>
        {
            var matchingSku = FindMatchingSku(activeSkus, combo, axisKeys);

            return new VariantOptionDto(
                Combination: combo,
                SkuId: matchingSku?.Id,
                SkuCode: matchingSku?.SkuCode,
                Price: matchingSku?.Price.Amount,
                Currency: matchingSku?.Price.Currency,
                ImageUrl: matchingSku?.ImageUrl,
                IsAvailable: matchingSku is not null);
        }).ToList();

        return new VariantMatrixDto(product.Id, product.Name, axes, options);
    }

    /// <summary>
    /// Gets distinct values for an axis from existing SKUs.
    /// Used when AllowedValues is empty (free-form Select).
    /// </summary>
    private static List<string> GetDistinctValuesFromSkus(
        IReadOnlyCollection<Sku> skus,
        string key)
    {
        return skus
            .Where(s => s.Status != SkuStatus.Deleted)
            .Select(s => s.TypedAttributes.GetValueOrDefault(key))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToList()!;
    }

    /// <summary>
    /// Generates all combinations from axes via Cartesian product.
    /// </summary>
    private static List<Dictionary<string, string>> GenerateCartesianProduct(
        List<VariantAxisDto> axes)
    {
        var result = new List<Dictionary<string, string>> { new(StringComparer.OrdinalIgnoreCase) };

        foreach (var axis in axes)
        {
            var newResult = new List<Dictionary<string, string>>();

            foreach (var existing in result)
            {
                foreach (var value in axis.Values)
                {
                    var combo = new Dictionary<string, string>(
                        existing, StringComparer.OrdinalIgnoreCase)
                    {
                        [axis.Key] = value
                    };
                    newResult.Add(combo);
                }
            }

            result = newResult;
        }

        return result;
    }

    /// <summary>
    /// Finds the SKU that matches a specific combination of variant values.
    /// </summary>
    private static Sku? FindMatchingSku(
        List<Sku> activeSkus,
        Dictionary<string, string> combo,
        List<string> axisKeys)
    {
        return activeSkus.FirstOrDefault(sku =>
            axisKeys.All(key =>
            {
                var skuValue = sku.TypedAttributes.GetValueOrDefault(key) ?? "";
                var comboValue = combo.GetValueOrDefault(key) ?? "";
                return string.Equals(skuValue, comboValue, StringComparison.OrdinalIgnoreCase);
            }));
    }
}
