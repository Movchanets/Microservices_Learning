using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

/// <summary>
/// A sellable variant of a Product. Child entity of the Product aggregate.
/// Carries its own Price, typed attributes (filterable), and flexible attributes (freeform).
/// </summary>
public sealed class Sku : Entity
{
    public Guid ProductId { get; private set; }
    public string SkuCode { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public SkuStatus Status { get; private set; } = SkuStatus.Active;

    /// <summary>
    /// Typed, filterable attributes (e.g., color, size, material).
    /// Stored as jsonb with GIN index for faceted search.
    /// Keys must match AttributeDefinition.Key for the product's category.
    /// </summary>
    public Dictionary<string, string> TypedAttributes { get; private set; } = [];

    /// <summary>
    /// Flexible, non-filterable attributes (e.g., care instructions, weight).
    /// Stored as jsonb without index.
    /// </summary>
    public Dictionary<string, string> FlexibleAttributes { get; private set; } = [];

    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private Sku() { }

    internal static Sku Create(
        Guid productId,
        string skuCode,
        Money price,
        Dictionary<string, string> typedAttributes,
        Dictionary<string, string>? flexibleAttributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        if (productId == Guid.Empty)
            throw new InvalidOperationException("ProductId is required for SKU creation");

        return new Sku
        {
            ProductId = productId,
            SkuCode = skuCode.Trim().ToUpperInvariant(),
            Price = price,
            TypedAttributes = typedAttributes ?? [],
            FlexibleAttributes = flexibleAttributes ?? [],
            Status = SkuStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ChangePrice(Money newPrice)
    {
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAttributes(
        Dictionary<string, string> typedAttributes,
        Dictionary<string, string>? flexibleAttributes = null)
    {
        TypedAttributes = typedAttributes ?? TypedAttributes;
        FlexibleAttributes = flexibleAttributes ?? FlexibleAttributes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the SKU as deleted. Only callable by the parent Product aggregate.
    /// </summary>
    internal void MarkDeleted()
    {
        Status = SkuStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = SkuStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = SkuStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive => Status == SkuStatus.Active;
}
