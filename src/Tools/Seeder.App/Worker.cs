using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;
using Seeder.App.Seeders;

namespace Seeder.App;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly string _dataDirectory;

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _hostApplicationLifetime = hostApplicationLifetime;
        _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting database seeding process...");
            var httpClient = _httpClientFactory.CreateClient("ApiGateway");

            // 1. Wait for Gateway & Downstream Services to be ready
            await WaitForServicesReadyAsync(httpClient, stoppingToken);

            var userSeeder = new UserSeeder(httpClient, _logger);
            var storeSeeder = new StoreSeeder(httpClient, _logger);
            var categorySeeder = new CategorySeeder(httpClient, _logger);
            var productSeeder = new ProductSeeder(httpClient, _logger);
            var inventorySeeder = new InventorySeeder(httpClient, _logger);

            // 2. Seed Users & Get Tokens
            var users = await LoadJsonAsync<List<UserModel>>("users.json");
            var adminUser = users.First(u => u.Role == "Admin");
            var techSeller = users.First(u => u.Email.Contains("tech"));
            var homeSeller = users.First(u => u.Email.Contains("home"));

            // Admin is seeded by Identity DbContext on startup. We can login directly.
            var adminToken = await userSeeder.LoginAsync(adminUser.Email, adminUser.Password, stoppingToken);

            await userSeeder.EnsureUserExistsAsync(techSeller, stoppingToken);
            await userSeeder.EnsureUserExistsAsync(homeSeller, stoppingToken);
            
            // Promote sellers using the Admin token
            await userSeeder.PromoteSellersAsync(users, adminToken, stoppingToken);

            var techSellerToken = await userSeeder.LoginAsync(techSeller.Email, techSeller.Password, stoppingToken);
            var homeSellerToken = await userSeeder.LoginAsync(homeSeller.Email, homeSeller.Password, stoppingToken);

            _logger.LogInformation("Waiting for identity events to settle...");
            await Task.Delay(2000, stoppingToken);

            // 3. Seed Stores
            var stores = await LoadJsonAsync<List<StoreModel>>("stores.json");
            foreach (var store in stores)
            {
                var sellerToken = store.SellerEmail.Contains("tech") ? techSellerToken : homeSellerToken;
                var sellerId = await userSeeder.GetUserIdAsync(store.SellerEmail, adminToken, stoppingToken);

                if (sellerId != null)
                {
                    await storeSeeder.EnsureStoreExistsAsync(store, sellerId.Value, sellerToken, adminToken, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Could not find user ID for seller {Email}. Skipping store.", store.SellerEmail);
                }

                await Task.Delay(500, stoppingToken);
            }

            _logger.LogInformation("Waiting for store verification events to propagate...");
            await Task.Delay(3000, stoppingToken);

            // 4. Seed Categories
            var categoriesToSeed = await LoadJsonAsync<List<CategoryModel>>("categories.json");
            var existingCategories = await categorySeeder.GetExistingCategoriesAsync(stoppingToken);
            var resultCategories = new List<CategoryDto>(existingCategories);

            foreach (var category in categoriesToSeed)
            {
                var createdCategory = await categorySeeder.EnsureCategoryExistsAsync(category, adminToken, existingCategories, stoppingToken);
                if (createdCategory != null && !resultCategories.Any(c => c.Name == createdCategory.Name))
                {
                    resultCategories.Add(createdCategory);
                }
            }

            // 5. Seed Products
            var products = await LoadJsonAsync<List<ProductModel>>("products.json");
            var techStoreId = await productSeeder.GetStoreIdAsync("Tech Store", stoppingToken);
            var homeStoreId = await productSeeder.GetStoreIdAsync("Home Store", stoppingToken);

            var productIds = new Dictionary<string, (Guid StoreId, Guid ProductId)>();

            foreach (var product in products)
            {
                var token = product.StoreName.Contains("Tech") ? techSellerToken : homeSellerToken;
                var categoryId = resultCategories.FirstOrDefault(c => c.Name == product.CategoryName)?.Id;
                var storeId = product.StoreName.Contains("Tech") ? techStoreId : homeStoreId;

                if (categoryId == null || storeId == null)
                {
                    _logger.LogWarning("Skipping product {Name} due to missing category or store ID.", product.Name);
                    continue;
                }

                var productId = await productSeeder.EnsureProductExistsAsync(product, token, categoryId.Value, storeId.Value, stoppingToken);
                if (productId != null)
                {
                    productIds[product.Sku] = (storeId.Value, productId.Value);
                }

                await Task.Delay(500, stoppingToken);
            }

            // 6. Seed Inventory (direct upsert — no polling needed)
            foreach (var product in products)
            {
                if (!productIds.TryGetValue(product.Sku, out var ids))
                {
                    _logger.LogWarning("Skipping inventory for {Sku} — product not created.", product.Sku);
                    continue;
                }

                var token = product.StoreName.Contains("Tech") ? techSellerToken : homeSellerToken;
                await inventorySeeder.EnsureInventoryStockedAsync(product, token, ids.StoreId, ids.ProductId, stoppingToken);
            }

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

    private async Task WaitForServicesReadyAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("Waiting for API Gateway to become responsive...");
        var retries = 0;
        while (!ct.IsCancellationRequested && retries < 30)
        {
            try
            {
                var response = await client.GetAsync("/health", ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("API Gateway is ready.");
                    return;
                }
            }
            catch
            {
                // Ignore connection errors while starting up
            }
            retries++;
            await Task.Delay(2000, ct);
        }
        throw new Exception("API Gateway did not become responsive in time.");
    }

    private async Task<T> LoadJsonAsync<T>(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Seed data file not found: {path}");
        }

        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream) ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
}