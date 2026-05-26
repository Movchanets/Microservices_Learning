using Cart.Domain.Entities;

namespace Cart.Domain.Repositories;

public interface IProductPriceRepository
{
    Task<ProductPrice?> GetByIdAsync(Guid productId, CancellationToken ct = default);
    Task<ProductPrice?> GetBySkuIdAsync(Guid skuId, CancellationToken ct = default);
    Task<List<ProductPrice>> GetBySkuIdsAsync(IEnumerable<Guid> skuIds, CancellationToken ct = default);
    Task UpsertAsync(Guid productId, Guid skuId, string skuCode, string name, decimal price, string currency, Guid storeId, CancellationToken ct = default);
    void Add(ProductPrice productPrice);
    void Update(ProductPrice productPrice);
    void Remove(ProductPrice productPrice);
}
