using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.ValueObjects;
using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.UnitTests.Domain.Builders;

/// <summary>
/// A fluent builder for creating complex Catalog Domain object hierarchies
/// (Category -> AttributeDefinitions -> Product -> VariantAxes -> SKUs).
/// </summary>
public class CatalogDataBuilder
{
    private Category? _category;
    private Product? _product;

    public CatalogDataBuilder WithCategory(string name, string? description = null)
    {
        _category = Category.Create(name, description);
        SetId(_category);
        return this;
    }

    public CatalogDataBuilder WithAttributeDefinition(
        string key, 
        string displayName, 
        AttributeTarget target, 
        AttributeType valueType, 
        List<string>? allowedValues = null,
        bool isRequired = true)
    {
        if (_category == null) 
            throw new InvalidOperationException("Category must be created first. Call WithCategory().");

        var attr = _category.AddAttributeDefinition(
            key, 
            displayName, 
            target, 
            valueType, 
            isFilterable: true, 
            isRequired: isRequired, 
            sortOrder: _category.AttributeDefinitions.Count, 
            allowedValues: allowedValues);

        SetId(attr);

        return this;
    }

    public CatalogDataBuilder WithProduct(string name, Guid? storeId = null)
    {
        if (_category == null) 
            throw new InvalidOperationException("Category must be created first. Call WithCategory().");

        var catId = _category.Id != Guid.Empty ? _category.Id : Guid.NewGuid();
        _product = Product.Create(name, "Description", catId, storeId ?? Guid.NewGuid());
        SetId(_product);
        
        return this;
    }

    public CatalogDataBuilder WithVariantAxes(params string[] attributeKeys)
    {
        if (_category == null || _product == null) 
            throw new InvalidOperationException("Category and Product must be created first.");

        var axisIds = _category.AttributeDefinitions
            .Where(a => attributeKeys.Contains(a.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(a => Array.IndexOf(attributeKeys, a.Key))
            .Select(a => a.Id)
            .ToList();

        _product.SetVariantAxes(axisIds);
        
        // Populate navigation properties for Unit Tests
        var axesField = typeof(Product).GetField("_variantAxes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (axesField != null)
        {
            var axes = (List<ProductVariantAxis>)axesField.GetValue(_product)!;
            foreach (var axis in axes)
            {
                var attrDef = _category.AttributeDefinitions.FirstOrDefault(a => a.Id == axis.AttributeDefinitionId);
                if (attrDef != null)
                {
                    var prop = typeof(ProductVariantAxis).GetProperty("AttributeDefinition");
                    if (prop != null && prop.CanWrite)
                        prop.SetValue(axis, attrDef);
                    else
                    {
                        var field = typeof(ProductVariantAxis).GetField("<AttributeDefinition>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (field != null) field.SetValue(axis, attrDef);
                    }
                }
            }
        }
        
        return this;
    }

    public CatalogDataBuilder WithSku(string skuCode, decimal price, Dictionary<string, string> typedAttributes)
    {
        if (_product == null) 
            throw new InvalidOperationException("Product must be created first.");

        var sku = _product.AddSku(skuCode, Money.Create(price, "USD"), typedAttributes);
        SetId(sku);
        
        // When setting IDs retroactively, attributes inside SKU also need the SKU ID updated
        // But for unit tests, simply generating the SKU ID before modifying its attributes is enough.
        
        return this;
    }

    public Category BuildCategory() => _category ?? throw new InvalidOperationException("Category not initialized");
    public Product BuildProduct() => _product ?? throw new InvalidOperationException("Product not initialized");
    
    /// <summary>
    /// Helps to quickly retrieve a created SKU.
    /// </summary>
    public Sku GetSku(string skuCode)
    {
        if (_product == null) throw new InvalidOperationException("Product not initialized");
        return _product.Skus.First(s => s.SkuCode == skuCode.ToUpperInvariant());
    }

    private static void SetId(object entity)
    {
        if (entity == null) return;
        
        var idProp = entity.GetType().GetProperty("Id");
        if (idProp != null && (Guid?)idProp.GetValue(entity) == Guid.Empty)
        {
            if (idProp.CanWrite)
            {
                idProp.SetValue(entity, Guid.NewGuid());
            }
            else
            {
                var field = entity.GetType().GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                         ?? entity.GetType().BaseType?.GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                         
                if (field != null) field.SetValue(entity, Guid.NewGuid());
            }
        }
    }
}
