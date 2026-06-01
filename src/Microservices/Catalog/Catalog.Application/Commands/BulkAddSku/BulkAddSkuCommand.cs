using BuildingBlocks.Infrastructure.Models;
using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Commands.BulkAddSku;

/// <summary>
/// Creates all SKU combinations from variant-axis attributes via Cartesian product.
/// For a category with Color=[Red,Blue] and Storage=[128GB,256GB], generates 4 SKUs.
/// </summary>
public sealed record BulkAddSkuCommand(
    Guid ProductId,

    /// <summary>
    /// Variant axes and their allowed values.
    /// Keys must match AttributeDefinition.Key where IsVariantAxis=true.
    /// Example: { "color": ["Black","White","Blue"], "storage": ["128GB","256GB","512GB"] }
    /// </summary>
    Dictionary<string, List<string>> VariantCombinations,

    /// <summary>
    /// Optional: apply the same price to all generated SKUs.
    /// If null, SKUs are created with price 0 (seller updates later).
    /// </summary>
    decimal? BasePrice = null,

    /// <summary>
    /// Optional: currency for the price. Defaults to "USD".
    /// </summary>
    string Currency = "USD",

    /// <summary>
    /// Optional: specific combinations to skip.
    /// Format: ["color:Blue,storage:512GB"] — values are comma-separated key:value pairs.
    /// </summary>
    List<string>? ExcludedCombinations = null,

    /// <summary>
    /// Optional: prefix for SKU codes. If null, uses product name abbreviation.
    /// Generated code format: {Prefix}-{Val1}-{Val2} (e.g., "IPH17-BLK-128")
    /// </summary>
    string? SkuCodePrefix = null
) : IRequest<Result<BulkAddSkuResultDto>>;
