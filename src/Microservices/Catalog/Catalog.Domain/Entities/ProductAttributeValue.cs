using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Holds base attribute values shared across all variants of a product.
/// </summary>
public sealed class ProductAttributeValue : Entity
{
    public Guid ProductId { get; private set; }
    public Guid AttributeDefinitionId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    // Navigation properties
    public AttributeDefinition? AttributeDefinition { get; private set; }

    // EF Core constructor
    private ProductAttributeValue() { }

    public static ProductAttributeValue Create(Guid productId, Guid attributeDefinitionId, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (productId == Guid.Empty)
            throw new InvalidOperationException("ProductId is required");
        if (attributeDefinitionId == Guid.Empty)
            throw new InvalidOperationException("AttributeDefinitionId is required");

        return new ProductAttributeValue
        {
            ProductId = productId,
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
