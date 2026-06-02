using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 2: Create stores for each seller and verify them.
/// Populates SellerRegistry with store IDs.
/// </summary>
public class StoreStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;
    private readonly SellerRegistry _sellers;

    public StoreStep(HttpClient client, ILogger logger, string dataDirectory, SellerRegistry sellers)
    {
        _client = client;
        _logger = logger;
        _dataDirectory = dataDirectory;
        _sellers = sellers;
    }

    public async Task ExecuteAsync(string adminToken, CancellationToken ct)
    {
        var storeSeeder = new StoreSeeder(_client, _logger);
        var userSeeder = new UserSeeder(_client, _logger);
        var stores = await SeedDataLoader.LoadJsonAsync<List<StoreModel>>(_dataDirectory, "stores.json");

        foreach (var store in stores)
        {
            var sellerCtx = _sellers.GetByEmail(store.SellerEmail);
            if (sellerCtx == null)
            {
                _logger.LogWarning("No seller context for {Email}. Skipping store.", store.SellerEmail);
                continue;
            }

            var sellerId = await userSeeder.GetUserIdAsync(store.SellerEmail, adminToken, ct);
            if (sellerId == null)
            {
                _logger.LogWarning("Could not find user ID for seller {Email}. Skipping store.", store.SellerEmail);
                continue;
            }

            // Create + verify store
            await storeSeeder.EnsureStoreExistsAsync(
                store, sellerId.Value, sellerCtx.Token, adminToken, ct);

            // Look up the store ID and register it
            var storeId = await storeSeeder.GetStoreIdAsync(store.Name, sellerCtx.Token, ct);
            if (storeId != null)
                _sellers.SetStoreId(store.SellerEmail, storeId.Value);

            await Task.Delay(500, ct);
        }
    }
}
