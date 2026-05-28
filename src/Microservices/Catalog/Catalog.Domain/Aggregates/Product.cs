using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Events;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Aggregates;

/// <summary>
/// The Product aggregate root. Represents the definition of what something is.
/// SKUs (sellable variants) are child entities within this aggregate boundary.
/// Price lives on SKU, not on Product — a t-shirt (Product) has multiple prices per variant.
/// </summary>
public sealed class Product : AggregateRoot
{
    // ── Identity ──
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? ImageUrl { get; private set; }
    public List<string> Tags { get; private set; } = [];
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;

    // ── Relationships ──
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public Guid StoreId { get; private set; }

    // ── Children ──
    private readonly List<Sku> _skus = [];
    public IReadOnlyCollection<Sku> Skus => _skus.AsReadOnly();

    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    // ── Computed ──
    public bool IsActive => Status == ProductStatus.Active;

    /// <summary>
    /// Returns the price of the first active SKU, or null if no active SKUs exist.
    /// Useful for display purposes (product listing cards).
    /// </summary>
    public Money? DefaultPrice => _skus.FirstOrDefault(s => s.IsActive)?.Price;

    // EF Core constructor
    private Product() { }

    public static Product Create(
        string name,
        string description,
        Guid categoryId,
        Guid storeId,
        string? brand = null,
        List<string>? tags = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (categoryId == Guid.Empty)
            throw new InvalidOperationException("CategoryId is required");
        if (storeId == Guid.Empty)
            throw new InvalidOperationException("StoreId is required");

        var product = new Product
        {
            Name = name.Trim(),
            Description = description.Trim(),
            Brand = brand?.Trim(),
            CategoryId = categoryId,
            StoreId = storeId,
            Tags = tags ?? [],
            ImageUrl = imageUrl,
            Status = ProductStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        product.AddDomainEvent(new ProductCreatedDomainEvent(
            product.Id, product.Name, product.Description,
            product.CategoryId, product.Tags, product.ImageUrl,
            product.Brand, product.StoreId, product.CreatedAt));

        return product;
    }

    public void Update(
        string name,
        string description,
        Guid categoryId,
        string? brand = null,
        List<string>? tags = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        Brand = brand?.Trim() ?? Brand;
        Tags = tags ?? Tags;
        ImageUrl = imageUrl ?? ImageUrl;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedDomainEvent(
            Id, Name, Description, CategoryId,
            Tags, ImageUrl, Brand, StoreId,
            IsActive, UpdatedAt.Value));
    }

    /// <summary>
    /// Adds a new SKU (sellable variant) to this product.
    /// </summary>
    public Sku AddSku(
        string skuCode,
        Money price,
        Dictionary<string, string> typedAttributes,
        Dictionary<string, string>? flexibleAttributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);

        var normalizedCode = skuCode.Trim().ToUpperInvariant();
        if (_skus.Any(s => s.SkuCode == normalizedCode && s.Status != SkuStatus.Deleted))
            throw new InvalidOperationException($"SKU '{normalizedCode}' already exists on this product");

        var sku = Sku.Create(Id, normalizedCode, price, typedAttributes, flexibleAttributes);
        _skus.Add(sku);

        AddDomainEvent(new SkuCreatedDomainEvent(
            Id, sku.Id, sku.SkuCode, Name, StoreId,
            price.Amount, price.Currency,
            typedAttributes ?? [], flexibleAttributes ?? []));

        return sku;
    }

    /// <summary>
    /// Soft-deletes a SKU from this product. The SKU is marked as Deleted, not removed from the collection.
    /// </summary>
    public void RemoveSku(Guid skuId)
    {
        var sku = _skus.FirstOrDefault(s => s.Id == skuId)
            ?? throw new InvalidOperationException($"SKU {skuId} not found on product {Id}");

        if (sku.Status == SkuStatus.Deleted)
            throw new InvalidOperationException($"SKU {skuId} is already deleted");

        sku.MarkDeleted();

        AddDomainEvent(new SkuDeletedDomainEvent(Id, sku.Id, sku.SkuCode));
    }

    /// <summary>
    /// Returns a specific SKU by ID. Throws if not found or deleted.
    /// </summary>
    public Sku GetSku(Guid skuId)
    {
        return _skus.FirstOrDefault(s => s.Id == skuId && s.Status != SkuStatus.Deleted)
            ?? throw new InvalidOperationException($"SKU {skuId} not found on product {Id}");
    }

    /// <summary>
    /// Changes the price of a specific SKU and fires a domain event.
    /// This is the aggregate-level operation that ensures event consistency.
    /// </summary>
    public void ChangeSkuPrice(Guid skuId, Money newPrice)
    {
        var sku = GetSku(skuId);
        var oldPrice = sku.Price;

        // No-op if price is unchanged
        if (oldPrice.Amount == newPrice.Amount && oldPrice.Currency == newPrice.Currency)
            return;

        sku.ChangePrice(newPrice);

        AddDomainEvent(new SkuPriceChangedDomainEvent(
            Id, sku.Id, sku.SkuCode,
            oldPrice.Amount, newPrice.Amount, newPrice.Currency));
    }

    public void Activate()
    {
        if (Status == ProductStatus.Deleted)
            throw new InvalidOperationException("Cannot activate a deleted product");

        if (!_skus.Any(s => s.IsActive))
            throw new InvalidOperationException("Product must have at least one active SKU to be activated");

        Status = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedDomainEvent(
            Id, Name, Description, CategoryId,
            Tags, ImageUrl, Brand, StoreId,
            IsActive, UpdatedAt.Value));
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedDomainEvent(
            Id, Name, Description, CategoryId,
            Tags, ImageUrl, Brand, StoreId,
            IsActive, UpdatedAt.Value));
    }

    public void SoftDelete()
    {
        Status = ProductStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductDeletedDomainEvent(Id));
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
