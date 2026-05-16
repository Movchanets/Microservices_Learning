using BuildingBlocks.SharedContracts.Abstractions;

namespace Catalog.Domain.Aggregates;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
    Task<List<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<List<Product>> GetByStoreAsync(Guid storeId, CancellationToken ct = default);
}
