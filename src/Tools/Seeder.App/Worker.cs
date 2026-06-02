using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Pipeline;

namespace Seeder.App;

/// <summary>
/// Thin orchestrator — delegates each step to its own pipeline class.
///
/// Pipeline order:
///   1. UserStep        — create admin + sellers, get auth tokens
///   2. StoreStep       — create stores for each seller, verify
///   3. CategoryStep    — create from categories.json + Rozetka breadcrumbs
///   4. AttributeStep   — create attribute definitions (static + VariantAxes)
///   5. ProductStep     — create products + variant SKUs with per-SKU galleries
///   6. InventoryStep   — stock initial quantities
///   7. MediaStep       — upload product/SKU gallery images
///   8. OrderFlowStep   — run a sample buyer checkout
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly string _dataDirectory;

    public Worker(
        ILogger<Worker> logger,
        IHttpClientFactory httpClientFactory,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _hostApplicationLifetime = hostApplicationLifetime;
        _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting database seeding process...");
            var httpClient = _httpClientFactory.CreateClient("ApiGateway");

            await SeedDataLoader.WaitForGatewayAsync(httpClient, _logger, ct);

            var sellers = new SellerRegistry(_logger);

            // ── Step 1: Users & Auth Tokens ──────────────────────
            var userStep = new UserStep(httpClient, _logger, _dataDirectory, sellers);
            var adminToken = await userStep.ExecuteAsync(ct);

            // ── Step 2: Stores ───────────────────────────────────
            var storeStep = new StoreStep(httpClient, _logger, _dataDirectory, sellers);
            await storeStep.ExecuteAsync(adminToken, ct);

            _logger.LogInformation("Waiting for store verification events to propagate...");
            await Task.Delay(3000, ct);

            // ── Step 3: Categories ───────────────────────────────
            var products = await SeedDataLoader.LoadJsonAsync<List<ProductModel>>(
                _dataDirectory, "products-v2.json");

            var categoryStep = new CategoryStep(httpClient, _logger, _dataDirectory);
            var categories = await categoryStep.ExecuteAsync(adminToken, products, ct);

            // ── Step 4: Attribute Definitions ────────────────────
            var attributeStep = new AttributeStep(httpClient, _logger, _dataDirectory);
            await attributeStep.ExecuteAsync(adminToken, products, categories, ct);

            // ── Step 5: Products + SKUs ──────────────────────────
            var categoryMapping = await SeedDataLoader.LoadCategoryMappingAsync(
                _dataDirectory, _logger);

            var productStep = new ProductStep(httpClient, _logger, sellers);
            var productIds = await productStep.ExecuteAsync(
                products, categories, categoryMapping, ct);

            // ── Step 6: Inventory ────────────────────────────────
            var inventoryStep = new InventoryStep(httpClient, _logger, sellers);
            await inventoryStep.ExecuteAsync(products, productIds, ct);

            // ── Step 7: Media Upload ─────────────────────────────
            var downloadClient = _httpClientFactory.CreateClient("download");
            var mediaClient = _httpClientFactory.CreateClient("MediaApi");

            var mediaStep = new MediaStep(
                httpClient, mediaClient, downloadClient,
                _logger, _dataDirectory, sellers);
            await mediaStep.ExecuteAsync(products, productIds, ct);

            // ── Step 8: Order Flow ───────────────────────────────
            var orderFlowStep = new OrderFlowStep(httpClient, _logger, _dataDirectory);
            await orderFlowStep.ExecuteAsync(productIds, ct);

            _logger.LogInformation("Seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during seeding.");
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    }
}
