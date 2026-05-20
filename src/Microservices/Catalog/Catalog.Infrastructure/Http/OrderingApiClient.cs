using System.Net.Http.Json;
using Catalog.Application.Interfaces;

namespace Catalog.Infrastructure.Http;

public class OrderingApiClient(HttpClient httpClient) : IOrderingApiClient
{
    public async Task<bool> HasPurchasedAsync(string buyerId, Guid productId, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<HasPurchasedResponse>(
            $"/api/orders/has-purchased?buyerId={buyerId}&productId={productId}", ct);
        return response?.HasPurchased ?? false;
    }

    private record HasPurchasedResponse(bool HasPurchased);
}
