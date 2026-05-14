using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Entities;

public sealed class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    private static string GenerateSlug(string name) =>
        name.Trim()
            .ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("--", "-");
}
