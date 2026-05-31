using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Enums;

namespace Catalog.Domain.Entities;

/// <summary>
/// A product category. Categories define a hierarchy (via ParentCategoryId)
/// and bind AttributeDefinitions that declare which attributes products/SKUs
/// must or may have within this category.
/// </summary>
public sealed class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Attribute Definitions (category-attribute binding) ──
    private readonly List<AttributeDefinition> _attributeDefinitions = [];
    public IReadOnlyCollection<AttributeDefinition> AttributeDefinitions => _attributeDefinitions.AsReadOnly();

    // EF Core constructor
    private Category() { }

    public static Category Create(
        string name,
        string? description = null,
        Guid? parentCategoryId = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Category
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCategoryId = parentCategoryId,
            Slug = GenerateSlug(name),
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
        Slug = GenerateSlug(name);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    /// <summary>
    /// Adds an attribute definition to this category.
    /// </summary>
    public AttributeDefinition AddAttributeDefinition(
        string key,
        string displayName,
        AttributeTarget target,
        AttributeType valueType,
        bool isFilterable,
        bool isRequired,
        int sortOrder = 0,
        List<string>? allowedValues = null)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();
        if (_attributeDefinitions.Any(a => a.Key == normalizedKey))
            throw new InvalidOperationException($"Attribute '{key}' already exists for category '{Name}'");

        var attr = AttributeDefinition.Create(
            Id, key, displayName, target, valueType, isFilterable, isRequired, sortOrder, allowedValues);
        _attributeDefinitions.Add(attr);
        return attr;
    }

    /// <summary>
    /// Removes an attribute definition from this category.
    /// </summary>
    public void RemoveAttributeDefinition(Guid attributeId)
    {
        var attr = _attributeDefinitions.FirstOrDefault(a => a.Id == attributeId)
            ?? throw new InvalidOperationException($"Attribute definition {attributeId} not found on category '{Name}'");
        _attributeDefinitions.Remove(attr);
    }

    /// <summary>
    /// Returns attribute definitions filtered by target (Product or SKU).
    /// </summary>
    public IReadOnlyList<AttributeDefinition> GetAttributeDefinitions(AttributeTarget target)
    {
        return _attributeDefinitions
            .Where(a => a.Target == target)
            .OrderBy(a => a.SortOrder)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Validates that a set of attributes satisfies all required definitions for this category.
    /// </summary>
    public void ValidateRequiredAttributes(
        AttributeTarget target,
        Dictionary<string, string> typedAttributes,
        Dictionary<string, string> flexibleAttributes)
    {
        var requiredDefs = _attributeDefinitions
            .Where(a => a.Target == target && a.IsRequired)
            .ToList();

        foreach (var def in requiredDefs)
        {
            var value = def.IsFilterable
                ? typedAttributes.GetValueOrDefault(def.Key)
                : flexibleAttributes.GetValueOrDefault(def.Key);

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Required attribute '{def.DisplayName}' ({def.Key}) is missing for category '{Name}'");

            if (def.ValueType == AttributeType.Select && def.AllowedValues.Count > 0)
            {
                if (!def.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Attribute '{def.DisplayName}' value '{value}' is not allowed. " +
                        $"Allowed values: {string.Join(", ", def.AllowedValues)}");
            }
        }
    }

    private static string GenerateSlug(string name) =>
        name.Trim()
            .ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("--", "-");
}
