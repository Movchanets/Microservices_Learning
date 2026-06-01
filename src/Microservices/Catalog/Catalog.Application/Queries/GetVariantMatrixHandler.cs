using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.Application.Queries;

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

        // 2. Load category with attribute definitions
        var category = await categoryRepository.GetWithAttributeDefinitionsAsync(
            product.CategoryId, cancellationToken);

        if (category is null)
            return null;

        // 3. Get variant-axis definitions (IsVariantAxis=true, Target=Sku, ValueType=Select)
        var variantDefs = category.AttributeDefinitions
            .Where(a => a.Target == AttributeTarget.Sku
                && a.IsVariantAxis
                && a.ValueType == AttributeType.Select)
            .OrderBy(a => a.SortOrder)
            .ToList();

        if (variantDefs.Count == 0)
            return new VariantMatrixDto(product.Id, product.Name, [], []);

        // 4. Build axes with their allowed values
        var axes = variantDefs
            .Select(def => new VariantAxisDto(
                def.Key,
                def.DisplayName,
                def.AllowedValues.Count > 0
                    ? def.AllowedValues
                    : GetDistinctValuesFromSkus(product.Skus, def.Key)))
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
