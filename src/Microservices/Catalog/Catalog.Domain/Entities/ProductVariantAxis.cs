using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Links a Product to an AttributeDefinition as a variant axis.
/// Each product defines its own set of variant axes (e.g., Color + Size),
/// independent of other products in the same category.
/// Child entity within the Product aggregate boundary.
/// </summary>
public sealed class ProductVariantAxis : Entity
{
    public Guid ProductId { get; private set; }
    public Guid AttributeDefinitionId { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation property
    public AttributeDefinition? AttributeDefinition { get; private set; }

    // EF Core constructor
    private ProductVariantAxis() { }

    internal static ProductVariantAxis Create(
        Guid productId,
        Guid attributeDefinitionId,
        int sortOrder)
    {
        if (attributeDefinitionId == Guid.Empty)
            throw new InvalidOperationException("AttributeDefinitionId is required");

        return new ProductVariantAxis
        {
            ProductId = productId,
            AttributeDefinitionId = attributeDefinitionId,
            SortOrder = sortOrder
        };
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
