using Cart.Domain.Entities;

namespace Cart.Domain.Repositories;

public interface IProductPriceRepository
{
    Task<ProductPrice?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<ProductPrice?> GetByIdAsync(Guid productId, CancellationToken ct = default);
    void Add(ProductPrice productPrice);
    void Update(ProductPrice productPrice);
    void Remove(ProductPrice productPrice);
}
