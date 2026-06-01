using BuildingBlocks.Infrastructure.Models;
using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.Catalog;
using Catalog.Application.DTOs;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;
using MassTransit;
using MediatR;

namespace Catalog.Application.Commands.BulkAddSku;

public sealed class BulkAddSkuHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<BulkAddSkuCommand, Result<BulkAddSkuResultDto>>
{
    public async Task<Result<BulkAddSkuResultDto>> Handle(
        BulkAddSkuCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load product with SKUs
        var product = await productRepository.GetWithSkusAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<BulkAddSkuResultDto>.Failure("Product not found", "NOT_FOUND");

        // 2. Load category with attribute definitions
        var category = await categoryRepository.GetWithAttributeDefinitionsAsync(
            product.CategoryId, cancellationToken);

        if (category is null)
            return Result<BulkAddSkuResultDto>.Failure("Category not found", "NOT_FOUND");

        // 3. Get variant-axis definitions and validate inputs
        var variantDefs = category.AttributeDefinitions
            .Where(a => a.Target == AttributeTarget.Sku && a.IsVariantAxis)
            .ToList();

        if (variantDefs.Count == 0)
            return Result<BulkAddSkuResultDto>.Failure(
                "Category has no variant-axis attributes defined. " +
                "Add AttributeDefinitions with IsVariantAxis=true first.",
                "NO_VARIANT_AXES");

        // Validate that all requested keys are valid variant axes
        var validKeys = variantDefs.Select(d => d.Key).ToHashSet();
        foreach (var key in request.VariantCombinations.Keys)
        {
            if (!validKeys.Contains(key))
                return Result<BulkAddSkuResultDto>.Failure(
                    $"'{key}' is not a variant-axis attribute. Valid axes: {string.Join(", ", validKeys)}",
                    "INVALID_AXIS");
        }

        // Validate that all values are in AllowedValues for each axis
        foreach (var (key, values) in request.VariantCombinations)
        {
            var def = variantDefs.First(d => d.Key == key);
            if (def.AllowedValues.Count == 0) continue;

            foreach (var value in values)
            {
                if (!def.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                    return Result<BulkAddSkuResultDto>.Failure(
                        $"'{value}' is not allowed for attribute '{def.DisplayName}'. " +
                        $"Allowed: {string.Join(", ", def.AllowedValues)}",
                        "INVALID_VALUE");
            }
        }

        // 4. Generate Cartesian product
        var combinations = GenerateCartesianProduct(request.VariantCombinations);

        // 5. Filter excluded combinations
        var excludedSet = ParseExcludedCombinations(request.ExcludedCombinations);
        var filteredCombinations = combinations
            .Where(combo => !IsExcluded(combo, excludedSet))
            .ToList();

        // 6. Determine SKU code prefix
        var prefix = request.SkuCodePrefix
            ?? GeneratePrefix(product.Name);

        // 7. Generate SKU codes and create SKUs
        var variantAxisKeys = variantDefs.Select(d => d.Key).ToList();
        var price = request.BasePrice.HasValue
            ? Money.Create(request.BasePrice.Value, request.Currency)
            : Money.Create(0, request.Currency);

        var createdSkus = new List<SkuDto>();
        var errors = new List<string>();
        var skusToPublish = new List<(Sku Sku, Product Product)>();

        foreach (var combo in filteredCombinations)
        {
            var skuCode = GenerateSkuCode(prefix, combo);
            var typedAttributes = combo.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

            try
            {
                var sku = product.AddSku(
                    skuCode, price, typedAttributes,
                    variantAxisKeys: variantAxisKeys);

                product.ClearDomainEvents();
                skusToPublish.Add((sku, product));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"SKU '{skuCode}': {ex.Message}");
            }
        }

        // 8. Save all SKUs in a single transaction
        if (skusToPublish.Count > 0)
        {
            productRepository.Update(product);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 9. Publish all integration events after successful save
            foreach (var (sku, prod) in skusToPublish)
            {
                await publishEndpoint.Publish(new SkuCreatedIntegrationEvent(
                    ProductId: prod.Id,
                    SkuId: sku.Id,
                    SkuCode: sku.SkuCode,
                    ProductName: prod.Name,
                    StoreId: prod.StoreId,
                    Price: sku.Price.Amount,
                    Currency: sku.Price.Currency,
                    TypedAttributes: sku.TypedAttributes,
                    FlexibleAttributes: sku.FlexibleAttributes,
                    Timestamp: DateTime.UtcNow), cancellationToken);

                createdSkus.Add(new SkuDto(
                    sku.Id,
                    sku.SkuCode,
                    sku.Price.Amount,
                    sku.Price.Currency,
                    sku.Status.ToString(),
                    sku.ImageUrl,
                    sku.TypedAttributes,
                    sku.FlexibleAttributes,
                    sku.CreatedAt));
            }
        }

        return Result<BulkAddSkuResultDto>.Success(new BulkAddSkuResultDto(
            createdSkus.Count,
            filteredCombinations.Count,
            createdSkus,
            errors.Count > 0 ? errors : null));
    }

    /// <summary>
    /// Generates all combinations from variant axes via Cartesian product.
    /// </summary>
    private static List<Dictionary<string, string>> GenerateCartesianProduct(
        Dictionary<string, List<string>> axes)
    {
        var result = new List<Dictionary<string, string>> { new(StringComparer.OrdinalIgnoreCase) };

        foreach (var (key, values) in axes)
        {
            var newResult = new List<Dictionary<string, string>>();

            foreach (var existing in result)
            {
                foreach (var value in values)
                {
                    var combo = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase)
                    {
                        [key] = value
                    };
                    newResult.Add(combo);
                }
            }

            result = newResult;
        }

        return result;
    }

    /// <summary>
    /// Parses excluded combinations from string format to a set for fast lookup.
    /// Input: ["color:Blue,storage:512GB"] → Set of normalized signatures.
    /// </summary>
    private static HashSet<string> ParseExcludedCombinations(List<string>? excluded)
    {
        if (excluded is null || excluded.Count == 0)
            return [];

        return excluded
            .Select(e => NormalizeCombinationSignature(
                e.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part =>
                    {
                        var kv = part.Split(':', 2);
                        return (Key: kv[0].Trim().ToLowerInvariant(),
                                Value: kv.Length > 1 ? kv[1].Trim() : "");
                    })
                    .ToDictionary(x => x.Key, x => x.Value)))
            .ToHashSet();
    }

    private static bool IsExcluded(
        Dictionary<string, string> combo,
        HashSet<string> excludedSet)
    {
        return excludedSet.Contains(NormalizeCombinationSignature(combo));
    }

    private static string NormalizeCombinationSignature(Dictionary<string, string> combo)
    {
        return string.Join("|", combo
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}={kvp.Value.ToUpperInvariant()}"));
    }

    /// <summary>
    /// Generates a SKU code from prefix + attribute values.
    /// Example: prefix="IPH17", combo={color:Black, storage:128GB} → "IPH17-BLK-128GB"
    /// </summary>
    private static string GenerateSkuCode(
        string prefix,
        Dictionary<string, string> combo)
    {
        var parts = combo
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => AbbreviateValue(kvp.Value));
        return $"{prefix}-{string.Join("-", parts)}".ToUpperInvariant();
    }

    /// <summary>
    /// Abbreviates a value for use in SKU codes.
    /// "Black" → "BLK", "128GB" → "128GB", "Extra Large" → "XL"
    /// </summary>
    private static string AbbreviateValue(string value)
    {
        var trimmed = value.Trim();

        // If it's already short (≤5 chars) or contains numbers, use as-is
        if (trimmed.Length <= 5 || trimmed.Any(char.IsDigit))
            return trimmed.ToUpperInvariant();

        // Try common abbreviations
        return trimmed.ToUpperInvariant() switch
        {
            "BLACK" => "BLK",
            "WHITE" => "WHT",
            "BLUE" => "BLU",
            "GREEN" => "GRN",
            "YELLOW" => "YEL",
            "ORANGE" => "ORG",
            "PURPLE" => "PUR",
            "RED" => "RED",
            "PINK" => "PNK",
            "GREY" => "GRY",
            "GRAY" => "GRY",
            "BROWN" => "BRN",
            "SILVER" => "SLV",
            "GOLD" => "GLD",
            "EXTRA SMALL" => "XS",
            "EXTRA LARGE" => "XL",
            "SMALL" => "S",
            "MEDIUM" => "M",
            "LARGE" => "L",
            // Default: first 3 chars
            _ => trimmed.Length >= 3
                ? trimmed[..3].ToUpperInvariant()
                : trimmed.ToUpperInvariant()
        };
    }

    private static string GeneratePrefix(string productName)
    {
        // Take first letter of each word, max 6 chars
        var words = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var prefix = string.Concat(words.Select(w => char.ToUpperInvariant(w[0])));
        return prefix.Length > 6 ? prefix[..6] : prefix;
    }
}
