using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Entities;

public sealed class ProductPrice : Entity
{
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTime UpdatedAt { get; private set; }

    private ProductPrice() { }

    public static ProductPrice Create(Guid productId, string sku, string name, decimal price, string currency)
    {
        return new ProductPrice
        {
            Id = productId,
            Sku = sku,
            Name = name,
            Price = price,
            Currency = currency,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePrice(decimal newPrice, string currency)
    {
        Price = newPrice;
        Currency = currency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, decimal price, string currency)
    {
        Name = name;
        Price = price;
        Currency = currency;
        UpdatedAt = DateTime.UtcNow;
    }
}
