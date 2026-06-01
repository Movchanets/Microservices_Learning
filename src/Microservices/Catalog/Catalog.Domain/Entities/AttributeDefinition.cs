using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Enums;

namespace Catalog.Domain.Entities;

/// <summary>
/// Defines an allowed attribute for products or SKUs within a category.
/// Supports the category-attribute binding pattern: each category declares
/// which attributes its products/SKUs must or may have.
/// </summary>
public sealed class AttributeDefinition : Entity
{
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Machine-readable key (e.g., "color", "size", "material").
    /// Unique per category.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Color", "Size", "Material").
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this attribute applies to Products or SKUs.
    /// </summary>
    public AttributeTarget Target { get; private set; }

    /// <summary>
    /// The value type for validation (Text, Number, Select).
    /// </summary>
    public AttributeType ValueType { get; private set; }

    /// <summary>
    /// If true, the attribute is stored in TypedAttributes (GIN-indexed for faceted search).
    /// If false, stored in FlexibleAttributes (no index).
    /// </summary>
    public bool IsFilterable { get; private set; }

    /// <summary>
    /// If true, every product/SKU in this category must provide this attribute.
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// If true, this attribute defines a variant axis (e.g., color, storage).
    /// Cartesian product of all variant-axis attributes generates SKU combinations.
    /// Only meaningful for Target=Sku with ValueType=Select.
    /// </summary>
    public bool IsVariantAxis { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>
    /// For Select-type attributes: the allowed values (e.g., ["Red", "Blue", "Green"]).
    /// Empty for Text/Number types.
    /// </summary>
    public List<string> AllowedValues { get; private set; } = [];

    // EF Core constructor
    private AttributeDefinition() { }

    public static AttributeDefinition Create(
        Guid categoryId,
        string key,
        string displayName,
        AttributeTarget target,
        AttributeType valueType,
        bool isFilterable,
        bool isRequired,
        int sortOrder = 0,
        List<string>? allowedValues = null,
        bool isVariantAxis = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (isVariantAxis && valueType != AttributeType.Select)
            throw new InvalidOperationException("Only Select-type attributes can be variant axes");

        return new AttributeDefinition
        {
            CategoryId = categoryId,
            Key = key.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Target = target,
            ValueType = valueType,
            IsFilterable = isFilterable,
            IsRequired = isRequired,
            SortOrder = sortOrder,
            AllowedValues = allowedValues ?? [],
            IsVariantAxis = isVariantAxis
        };
    }

    public void Update(
        string displayName,
        bool isFilterable,
        bool isRequired,
        int sortOrder,
        List<string>? allowedValues = null,
        bool isVariantAxis = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (isVariantAxis && ValueType != AttributeType.Select)
            throw new InvalidOperationException("Only Select-type attributes can be variant axes");

        DisplayName = displayName.Trim();
        IsFilterable = isFilterable;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        AllowedValues = allowedValues ?? AllowedValues;
        IsVariantAxis = isVariantAxis;
    }
}
