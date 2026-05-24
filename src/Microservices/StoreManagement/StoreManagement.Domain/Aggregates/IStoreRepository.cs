using BuildingBlocks.SharedContracts.Abstractions;

namespace StoreManagement.Domain.Aggregates;

public interface IStoreRepository : IRepository<Store>
{
    Task<Store?> GetBySellerIdAsync(string sellerId, CancellationToken ct = default);
    Task<bool> ExistsBySellerIdAsync(string sellerId, CancellationToken ct = default);
    Task<List<Store>> GetAllAsync(CancellationToken ct = default);
}
