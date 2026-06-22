using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Holds attribute values specific to a variant (SKU).
/// </summary>
public sealed class SkuAttributeValue : Entity
{
    public Guid SkuId { get; private set; }
    public Guid AttributeDefinitionId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    // Navigation properties
    public AttributeDefinition? AttributeDefinition { get; private set; }

    // EF Core constructor
    private SkuAttributeValue() { }

    public static SkuAttributeValue Create(Guid skuId, Guid attributeDefinitionId, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (skuId == Guid.Empty)
            throw new InvalidOperationException("SkuId is required");
        if (attributeDefinitionId == Guid.Empty)
            throw new InvalidOperationException("AttributeDefinitionId is required");

        return new SkuAttributeValue
        {
            SkuId = skuId,
            AttributeDefinitionId = attributeDefinitionId,
            Value = value.Trim()
        };
    }

    public void UpdateValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }
}
