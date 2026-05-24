using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

public class StoreSeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public StoreSeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnsureStoreExistsAsync(StoreModel store, Guid sellerId, string sellerToken, string adminToken, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sellerToken);

        Guid storeId;

        // Check if store exists
        List<StoreDto>? existingStores = null;
        var getStoresResponse = await _client.GetAsync("/api/stores", ct);
        if (getStoresResponse.IsSuccessStatusCode)
        {
            existingStores = await getStoresResponse.Content.ReadFromJsonAsync<List<StoreDto>>(cancellationToken: ct);
        }
        else
        {
            var error = await getStoresResponse.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to fetch existing stores: {StatusCode} - {Error}", getStoresResponse.StatusCode, error);
        }

        var existingStore = existingStores?.FirstOrDefault(s => s.Name == store.Name);

        if (existingStore != null)
        {
            _logger.LogInformation("Store already exists: {Name}", store.Name);
            storeId = existingStore.Id;

            if (existingStore.VerificationStatus == "Verified")
            {
                return;
            }
        }
        else
        {
            var requestBody = new { SellerId = sellerId.ToString(), store.Name, store.Description };
            var response = await _client.PostAsJsonAsync("/api/stores", requestBody, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Created store: {Name}", store.Name);

                // Get the ID of the newly created store. CreateStoreEndpoint returns StoreDto
                var createdStore = await response.Content.ReadFromJsonAsync<StoreDto>(cancellationToken: ct);
                if (createdStore == null)
                {
                    _logger.LogWarning("Could not read created store ID for {Name}.", store.Name);
                    return;
                }
                storeId = createdStore.Id;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to create store {Name}: {StatusCode} - {Error}", store.Name, response.StatusCode, error);
                return;
            }
        }

        // Verify the store
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var verifyResponse = await _client.PostAsync($"/api/stores/{storeId}/verify", null, ct);

        if (verifyResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Verified store: {Name}", store.Name);
        }
        else
        {
            var error = await verifyResponse.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to verify store {Name}: {StatusCode} - {Error}", store.Name, verifyResponse.StatusCode, error);
        }
    }
}