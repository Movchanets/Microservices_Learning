using BuildingBlocks.SharedContracts.Abstractions;

namespace Cart.Domain.Entities;

public sealed class ProductPrice : Entity
{
    public Guid ProductId { get; private set; }
    public Guid SkuId { get; private set; }
    public string SkuCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "USD";
    public Guid StoreId { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProductPrice() { }

    public static ProductPrice Create(Guid productId, Guid skuId, string skuCode, string name, decimal price, string currency, Guid storeId)
    {
        return new ProductPrice
        {
            ProductId = productId,
            SkuId = skuId,
            SkuCode = skuCode,
            Name = name,
            Price = price,
            Currency = currency,
            StoreId = storeId,
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
