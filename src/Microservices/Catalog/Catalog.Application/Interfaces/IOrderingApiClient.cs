namespace Catalog.Application.Interfaces;

public interface IOrderingApiClient
{
    Task<bool> HasPurchasedAsync(string buyerId, Guid productId, CancellationToken ct = default);
}
