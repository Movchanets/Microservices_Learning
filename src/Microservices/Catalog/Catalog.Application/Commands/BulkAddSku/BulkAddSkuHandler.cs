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

/// <summary>
/// Handles BulkAddSkuCommand: generates all SKU variants from the cartesian product of
/// variant axes (e.g., Color × Size), validates attribute values against Category definitions,
/// creates domain entities, publishes SkuCreatedIntegrationEvent for each new SKU,
/// and commits via Outbox in a single transaction.
/// </summary>
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
        // ── Load & Validate ──────────────────────────────────────────
        var (product, category, variantDefs, error) = await LoadAndValidateAsync(
            request.ProductId, cancellationToken);

        if (error is not null)
            return error;

        var validationError = ValidateVariantInputs(request, variantDefs!);
        if (validationError is not null)
            return validationError;

        // ── Generate Combinations ────────────────────────────────────
        var combinations = GenerateCartesianProduct(request.VariantCombinations);
        var excludedSet = ParseExcludedCombinations(request.ExcludedCombinations);
        var filteredCombinations = combinations
            .Where(combo => !excludedSet.Contains(NormalizeCombinationSignature(combo)))
            .ToList();

        // ── Create SKUs ──────────────────────────────────────────────
        var prefix = request.SkuCodePrefix ?? GeneratePrefix(product!.Name);
        var price = request.BasePrice.HasValue
            ? Money.Create(request.BasePrice.Value, request.Currency)
            : Money.Create(0, request.Currency);

        var (errors, skusToPublish) = CreateSkus(
            product!, filteredCombinations, prefix, price);

        // ── Save & Publish ───────────────────────────────────────────
        var createdSkus = new List<SkuDto>();

        if (skusToPublish.Count > 0)
        {
            productRepository.Update(product!);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            createdSkus = await PublishEventsAsync(skusToPublish, cancellationToken);
        }

        if (createdSkus.Count == 0 && errors.Count > 0)
            return Result<BulkAddSkuResultDto>.Failure(
                $"All {errors.Count} SKU creation(s) failed: {string.Join("; ", errors)}",
                "ALL_SKUS_FAILED");

        return Result<BulkAddSkuResultDto>.Success(new BulkAddSkuResultDto(
            createdSkus.Count,
            filteredCombinations.Count,
            createdSkus,
            errors.Count > 0 ? errors : null));
    }

    // ── Load & Validate ──────────────────────────────────────────────

    private async Task<(Product? Product, Category? Category, List<AttributeDefinition>? VariantDefs, Result<BulkAddSkuResultDto>? Error)>
        LoadAndValidateAsync(Guid productId, CancellationToken ct)
    {
        var product = await productRepository.GetWithSkusAsync(productId, ct);
        if (product is null)
            return (null, null, null, Result<BulkAddSkuResultDto>.Failure("Product not found", "NOT_FOUND"));

        var category = await categoryRepository.GetWithAttributeDefinitionsAsync(product.CategoryId, ct);
        if (category is null)
            return (null, null, null, Result<BulkAddSkuResultDto>.Failure("Category not found", "NOT_FOUND"));

        // Variant axes are now defined at the Product level
        var variantDefs = product.VariantAxes
            .OrderBy(a => a.SortOrder)
            .Select(a => a.AttributeDefinition)
            .Where(d => d is not null)
            .Cast<AttributeDefinition>()
            .ToList();

        if (variantDefs.Count == 0)
            return (null, null, null, Result<BulkAddSkuResultDto>.Failure(
                "Product has no variant-axis attributes defined. Use SetVariantAxes to configure variant axes first.",
                "NO_VARIANT_AXES"));

        return (product, category, variantDefs, null);
    }

    private static Result<BulkAddSkuResultDto>? ValidateVariantInputs(
        BulkAddSkuCommand request,
        List<AttributeDefinition> variantDefs)
    {
        var validKeys = variantDefs.Select(d => d.Key).ToHashSet();

        foreach (var key in request.VariantCombinations.Keys)
        {
            if (!validKeys.Contains(key))
                return Result<BulkAddSkuResultDto>.Failure(
                    $"'{key}' is not a variant-axis attribute. Valid axes: {string.Join(", ", validKeys)}",
                    "INVALID_AXIS");
        }

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

        return null;
    }

    // ── SKU Creation ─────────────────────────────────────────────────

    private static (List<string> Errors, List<(Sku, Product)> ToPublish)
        CreateSkus(
            Product product,
            List<Dictionary<string, string>> combinations,
            string prefix,
            Money price)
    {
        var errors = new List<string>();
        var skusToPublish = new List<(Sku Sku, Product Product)>();

        foreach (var combo in combinations)
        {
            var skuCode = GenerateSkuCode(prefix, combo);
            var typedAttributes = combo.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

            try
            {
                // Create a unique Money instance for each SKU to avoid EF Core change tracking issues
                var skuPrice = Money.Create(price.Amount, price.Currency);
                var sku = product.AddSku(skuCode, skuPrice, typedAttributes);
                product.ClearDomainEvents();
                skusToPublish.Add((sku, product));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"SKU '{skuCode}': {ex.Message}");
            }
        }

        return (errors, skusToPublish);
    }

    /// <summary>
    /// Publishes integration events for each created SKU and returns the DTOs.
    /// Must run after SaveChanges so sku.Id is populated by EF Core.
    /// </summary>
    private async Task<List<SkuDto>> PublishEventsAsync(
        List<(Sku Sku, Product Product)> skusToPublish,
        CancellationToken ct)
    {
        var createdSkus = new List<SkuDto>();

        foreach (var (sku, product) in skusToPublish)
        {
            await publishEndpoint.Publish(new SkuCreatedIntegrationEvent(
                ProductId: product.Id,
                SkuId: sku.Id,
                SkuCode: sku.SkuCode,
                ProductName: product.Name,
                StoreId: product.StoreId,
                Price: sku.Price.Amount,
                Currency: sku.Price.Currency,
                TypedAttributes: sku.TypedAttributes,
                FlexibleAttributes: sku.FlexibleAttributes,
                Timestamp: DateTime.UtcNow), ct);

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

        return createdSkus;
    }

    // ── Cartesian Product ────────────────────────────────────────────

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

    // ── Exclusion Parsing ────────────────────────────────────────────

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

    private static string NormalizeCombinationSignature(Dictionary<string, string> combo)
    {
        return string.Join("|", combo
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}={kvp.Value.ToUpperInvariant()}"));
    }

    // ── SKU Code Generation ──────────────────────────────────────────

    private static string GenerateSkuCode(string prefix, Dictionary<string, string> combo)
    {
        var parts = combo
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => AbbreviateValue(kvp.Value));
        return $"{prefix}-{string.Join("-", parts)}".ToUpperInvariant();
    }

    private static string AbbreviateValue(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();

        // Try common abbreviations first
        var abbr = trimmed switch
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
            _ => null
        };

        if (abbr != null) return abbr;

        // If no common abbreviation, and it's already short (≤5 chars) or contains numbers, use as-is
        if (trimmed.Length <= 5 || trimmed.Any(char.IsDigit))
            return trimmed;

        // Default: first 3 chars
        return trimmed.Length >= 3
            ? trimmed[..3]
            : trimmed;
    }

    private static string GeneratePrefix(string productName)
    {
        // Take first letter of each word, max 6 chars
        var words = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var prefix = string.Concat(words.Select(w => char.ToUpperInvariant(w[0])));
        return prefix.Length > 6 ? prefix[..6] : prefix;
    }
}
