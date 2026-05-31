using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Domain.Entities;

namespace Catalog.Domain.Aggregates;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetWithSkusAsync(Guid productId, CancellationToken ct = default);
    Task<Sku?> GetSkuByCodeAsync(string skuCode, CancellationToken ct = default);
    Task<bool> ExistsBySkuCodeAsync(string skuCode, CancellationToken ct = default);
    Task<List<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<List<Product>> GetByStoreAsync(Guid storeId, CancellationToken ct = default);
}
