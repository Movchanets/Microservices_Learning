using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Events;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Aggregates;

public sealed class Product : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public Sku Sku { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    public string? ImageUrl { get; private set; }
    public Guid SellerId { get; private set; }
    public List<string> Tags { get; private set; } = [];
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private Product() { }

    public static Product Create(
        string name,
        string description,
        decimal price,
        string currency,
        string sku,
        Guid categoryId,
        Guid sellerId,
        List<string>? tags = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required", nameof(categoryId));

        if (sellerId == Guid.Empty)
            throw new ArgumentException("SellerId is required", nameof(sellerId));

        var product = new Product
        {
            Name = name.Trim(),
            Description = description.Trim(),
            Price = Money.Create(price, currency),
            Sku = Sku.Create(sku),
            CategoryId = categoryId,
            SellerId = sellerId,
            Tags = tags ?? [],
            ImageUrl = imageUrl,
            Status = ProductStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        product.AddDomainEvent(new ProductCreatedDomainEvent(
            product.Id, product.Name, product.Sku.Value));

        return product;
    }

    public void Update(
        string name,
        string description,
        Guid categoryId,
        List<string>? tags = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        Tags = tags ?? Tags;
        ImageUrl = imageUrl ?? ImageUrl;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedDomainEvent(Id, Name));
    }

    public void ChangePrice(decimal newPrice, string currency)
    {
        var oldPrice = Price;
        Price = Money.Create(newPrice, currency);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductPriceChangedDomainEvent(
            Id, oldPrice.Amount, newPrice, currency));
    }

    public void Activate()
    {
        if (Status == ProductStatus.Deleted)
            throw new InvalidOperationException("Cannot activate a deleted product");

        Status = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductUpdatedDomainEvent(Id, Name));
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductUpdatedDomainEvent(Id, Name));
    }

    public void SoftDelete()
    {
        Status = ProductStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductDeletedDomainEvent(Id));
    }

    public bool IsActive => Status == ProductStatus.Active;
}
