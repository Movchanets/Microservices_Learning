using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App.Pipeline;

/// <summary>
/// Step 8: Run a sample buyer checkout flow to verify the full order pipeline.
/// </summary>
public class OrderFlowStep
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly string _dataDirectory;

    public OrderFlowStep(HttpClient client, ILogger logger, string dataDirectory)
    {
        _client = client;
        _logger = logger;
        _dataDirectory = dataDirectory;
    }

    public async Task ExecuteAsync(
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        CancellationToken ct)
    {
        _logger.LogInformation("Waiting for inventory events to propagate...");
        await Task.Delay(3000, ct);

        var users = await SeedDataLoader.LoadJsonAsync<List<UserModel>>(_dataDirectory, "users.json");
        var buyerUser = users.First(u => u.Role == "Buyer");
        var orderFlowSeeder = new OrderFlowSeeder(_client, _logger);

        try
        {
            var (buyerToken, buyerId) = await orderFlowSeeder.EnsureBuyerAsync(buyerUser, ct);
            _logger.LogInformation("Buyer ready: {BuyerId}", buyerId);
            await orderFlowSeeder.RunOrderFlowAsync(buyerToken, productIds, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order flow seeder failed (non-fatal).");
        }
    }
}
