using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

public class InventorySeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public InventorySeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnsureInventoryStockedAsync(ProductModel product, string token, Guid storeId, Guid productId, CancellationToken ct)
    {
        if (product.InitialStock <= 0) return;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            $"/api/inventory/items/{product.Sku}/stock",
            new { Quantity = product.InitialStock, StoreId = storeId, ProductId = productId },
            ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Set stock for {Sku} to {Quantity}", product.Sku, product.InitialStock);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Failed to set stock for {Sku}: {StatusCode} - {Error}", product.Sku, response.StatusCode, error);
        }
    }
}