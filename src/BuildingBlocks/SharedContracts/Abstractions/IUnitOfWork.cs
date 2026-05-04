namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Unit of Work interface. Implemented per-service wrapping DbContext.SaveChanges().
/// </summary>
public interface IUnitOfWork
{
	Task<int> SaveChangesAsync(CancellationToken ct = default);
}
