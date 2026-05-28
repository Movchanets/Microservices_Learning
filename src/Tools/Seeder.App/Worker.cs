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

            await WaitForServicesReadyAsync(httpClient, stoppingToken);

            var userSeeder = new UserSeeder(httpClient, _logger);
            var storeSeeder = new StoreSeeder(httpClient, _logger);
            var categorySeeder = new CategorySeeder(httpClient, _logger);
            var productSeeder = new ProductSeeder(httpClient, _logger);
            var inventorySeeder = new InventorySeeder(httpClient, _logger);

            // 1. Seed Users & get tokens
            var (adminToken, techSellerToken, homeSellerToken) = await SeedUsersAsync(userSeeder, stoppingToken);

            // 2. Seed Stores
            await SeedStoresAsync(storeSeeder, userSeeder, adminToken, techSellerToken, homeSellerToken, stoppingToken);

            _logger.LogInformation("Waiting for store verification events to propagate...");
            await Task.Delay(3000, stoppingToken);

            // 3. Seed Categories
            var products = await LoadJsonAsync<List<ProductModel>>("products.json");
            var resultCategories = await SeedCategoriesAsync(categorySeeder, adminToken, products, stoppingToken);

            // 4. Load category mapping
            var categoryMapping = await LoadCategoryMappingAsync();

            // 5. Seed Products
            var productIds = await SeedProductsAsync(
                productSeeder, products, resultCategories, categoryMapping,
                techSellerToken, homeSellerToken, stoppingToken);

            // 6. Seed Inventory
            await SeedInventoryAsync(inventorySeeder, products, productIds, techSellerToken, homeSellerToken, stoppingToken);

            // 7. Upload product images (direct to MediaApi, bypasses YARP gateway)
            var downloadClient = _httpClientFactory.CreateClient("download");
            var mediaClient = _httpClientFactory.CreateClient("MediaApi");
            var mediaSeeder = new MediaSeeder(httpClient, mediaClient, downloadClient, _logger, _dataDirectory);
            await UploadProductImagesAsync(mediaSeeder, products, productIds, techSellerToken, homeSellerToken, stoppingToken);

            // 8. Order flow
            await RunOrderFlowAsync(httpClient, productIds, stoppingToken);

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

    private async Task<(string AdminToken, string TechSellerToken, string HomeSellerToken)> SeedUsersAsync(
        UserSeeder userSeeder, CancellationToken ct)
    {
        var users = await LoadJsonAsync<List<UserModel>>("users.json");
        var adminUser = users.First(u => u.Role == "Admin");
        var techSeller = users.First(u => u.Email.Contains("tech"));
        var homeSeller = users.First(u => u.Email.Contains("home"));

        var adminToken = await userSeeder.LoginAsync(adminUser.Email, adminUser.Password, ct);
        await userSeeder.EnsureUserExistsAsync(techSeller, ct);
        await userSeeder.EnsureUserExistsAsync(homeSeller, ct);
        await userSeeder.PromoteSellersAsync(users, adminToken, ct);

        var techSellerToken = await userSeeder.LoginAsync(techSeller.Email, techSeller.Password, ct);
        var homeSellerToken = await userSeeder.LoginAsync(homeSeller.Email, homeSeller.Password, ct);

        _logger.LogInformation("Waiting for identity events to settle...");
        await Task.Delay(2000, ct);

        return (adminToken, techSellerToken, homeSellerToken);
    }

    private async Task SeedStoresAsync(
        StoreSeeder storeSeeder, UserSeeder userSeeder,
        string adminToken, string techSellerToken, string homeSellerToken, CancellationToken ct)
    {
        var stores = await LoadJsonAsync<List<StoreModel>>("stores.json");
        foreach (var store in stores)
        {
            var sellerToken = GetSellerToken(store.SellerEmail, techSellerToken, homeSellerToken);
            var sellerId = await userSeeder.GetUserIdAsync(store.SellerEmail, adminToken, ct);
            if (sellerId != null)
                await storeSeeder.EnsureStoreExistsAsync(store, sellerId.Value, sellerToken, adminToken, ct);
            else
                _logger.LogWarning("Could not find user ID for seller {Email}. Skipping store.", store.SellerEmail);
            await Task.Delay(500, ct);
        }
    }

    private async Task<List<CategoryDto>> SeedCategoriesAsync(
        CategorySeeder categorySeeder, string adminToken, List<ProductModel> rozetkaProducts, CancellationToken ct)
    {
        var categoriesToSeed = await LoadJsonAsync<List<CategoryModel>>("categories.json");
        var existingCategories = await categorySeeder.GetExistingCategoriesAsync(ct);
        var resultCategories = new List<CategoryDto>(existingCategories);

        foreach (var category in categoriesToSeed)
        {
            var createdCategory = await categorySeeder.EnsureCategoryExistsAsync(category, adminToken, existingCategories, ct);
            if (createdCategory != null && !resultCategories.Any(c => c.Name == createdCategory.Name))
                resultCategories.Add(createdCategory);
        }

        // Also create nested categories from Rozetka breadcrumbs
        // Parse breadcrumb paths like "Мобільні телефони > Мобільні телефони Apple > ..."
        // and create parent→child hierarchy
        var breadcrumbPaths = rozetkaProducts
            .Where(p => p.CategoryName.Contains('>'))
            .Select(p => p.CategoryName)
            .Distinct()
            .ToList();

        // Collect all unique segments in order (parents before children)
        var segmentOrder = new List<string>();
        foreach (var path in breadcrumbPaths)
        {
            var segments = path.Split('>').Select(s => s.Trim())
                .Where(s => s.Length > 2 && s != "Інтернет-магазин Rozetka")
                .ToList();
            foreach (var seg in segments)
            {
                if (!segmentOrder.Contains(seg, StringComparer.OrdinalIgnoreCase))
                    segmentOrder.Add(seg);
            }
        }

        // Create categories depth-first: parent first, then children
        foreach (var catName in segmentOrder)
        {
            if (resultCategories.Any(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Find parent: look for the breadcrumb path that contains this segment
            // and use the segment before it as the parent
            Guid? parentId = null;
            foreach (var path in breadcrumbPaths)
            {
                var segments = path.Split('>').Select(s => s.Trim()).ToList();
                var idx = segments.FindIndex(s => s.Equals(catName, StringComparison.OrdinalIgnoreCase));
                if (idx > 0)
                {
                    // The segment before this one is the parent
                    var parentName = segments[idx - 1].Trim();
                    if (parentName.Length > 2 && parentName != "Інтернет-магазин Rozetka")
                    {
                        var parentCat = resultCategories.FirstOrDefault(c =>
                            c.Name.Equals(parentName, StringComparison.OrdinalIgnoreCase));
                        if (parentCat != null)
                        {
                            parentId = parentCat.Id;
                            break;
                        }
                    }
                }
            }

            var created = await categorySeeder.EnsureCategoryExistsAsync(
                new CategoryModel(catName, $"Rozetka: {catName}", parentId),
                adminToken, existingCategories, ct);
            if (created != null)
                resultCategories.Add(created);
        }

        return resultCategories;
    }

    private async Task<Dictionary<string, string>> LoadCategoryMappingAsync()
    {
        try
        {
            var mappingPath = Path.Combine(_dataDirectory, "category-mapping.json");
            if (File.Exists(mappingPath))
            {
                var mappingJson = await File.ReadAllTextAsync(mappingPath);
                var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson) ?? new();
                _logger.LogInformation("Loaded {Count} category mappings", mapping.Count);
                return mapping;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load category mapping, using fallback matching");
        }
        return new();
    }

    private async Task<Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)>> SeedProductsAsync(
        ProductSeeder productSeeder,
        List<ProductModel> products,
        List<CategoryDto> categories,
        Dictionary<string, string> categoryMapping,
        string techSellerToken,
        string homeSellerToken,
        CancellationToken ct)
    {
        var techStoreId = await productSeeder.GetStoreIdAsync("Tech Store", ct);
        var homeStoreId = await productSeeder.GetStoreIdAsync("Home Store", ct);
        var productIds = new Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)>();

        foreach (var product in products)
        {
            var token = GetSellerToken(product.StoreName, techSellerToken, homeSellerToken);
            var storeId = GetStoreId(product.StoreName, techStoreId, homeStoreId);

            var categoryId = FindBestCategory(product.CategoryName, categories, categoryMapping);
            if (categoryId == null || storeId == null)
            {
                _logger.LogWarning("Skipping product {Name} - missing category or store.", product.Name);
                continue;
            }

            var result = await productSeeder.EnsureProductExistsAsync(product, token, categoryId.Value, storeId.Value, ct);
            if (result != null)
            {
                var (productId, skuIds) = result.Value;
                foreach (var (skuCode, skuId) in skuIds)
                    productIds[skuCode] = (storeId.Value, productId, skuId);
            }

            await Task.Delay(500, ct);
        }

        return productIds;
    }

    private async Task SeedInventoryAsync(
        InventorySeeder inventorySeeder,
        List<ProductModel> products,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        string techSellerToken,
        string homeSellerToken,
        CancellationToken ct)
    {
        foreach (var product in products)
        {
            if (productIds.TryGetValue(product.Sku, out var ids))
            {
                var token = GetSellerToken(product.StoreName, techSellerToken, homeSellerToken);
                await inventorySeeder.EnsureInventoryStockedAsync(product, token, ids.StoreId, ids.ProductId, ct);
            }

            if (product.Variants != null)
            {
                foreach (var variant in product.Variants)
                {
                    var variantSku = $"ROZ-{variant.RozetkaCode}";
                    if (productIds.TryGetValue(variantSku, out var variantIds))
                    {
                        var token = GetSellerToken(product.StoreName, techSellerToken, homeSellerToken);
                        await inventorySeeder.EnsureInventoryStockedAsync(
                            product with { Sku = variantSku }, token, variantIds.StoreId, variantIds.ProductId, ct);
                    }
                }
            }
        }
    }

    private async Task UploadProductImagesAsync(
        MediaSeeder mediaSeeder,
        List<ProductModel> products,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        string techSellerToken,
        string homeSellerToken,
        CancellationToken ct)
    {
        foreach (var product in products)
        {
            if (!productIds.TryGetValue(product.Sku, out var ids))
                continue;

            try
            {
                var token = GetSellerToken(product.StoreName, techSellerToken, homeSellerToken);
                await mediaSeeder.UploadProductAndVariantGalleriesAsync(
                    ids.ProductId, productIds.ToDictionary(k => k.Key, v => v.Value.SkuId), product, token, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Media upload failed for {Name} (non-fatal)", product.Name);
            }

            await Task.Delay(500, ct);
        }
    }

    private async Task RunOrderFlowAsync(
        HttpClient httpClient,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        CancellationToken ct)
    {
        _logger.LogInformation("Waiting for inventory events to propagate...");
        await Task.Delay(3000, ct);

        var users = await LoadJsonAsync<List<UserModel>>("users.json");
        var buyerUser = users.First(u => u.Role == "Buyer");
        var orderFlowSeeder = new OrderFlowSeeder(httpClient, _logger);
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

    /// <summary>
    /// Resolves seller token based on store name convention.
    /// </summary>
    private static string GetSellerToken(string sellerEmailOrStoreName, string techSellerToken, string homeSellerToken)
    {
        return sellerEmailOrStoreName.Contains("Tech", StringComparison.OrdinalIgnoreCase)
            ? techSellerToken
            : homeSellerToken;
    }

    /// <summary>
    /// Resolves store ID based on store name convention.
    /// </summary>
    private static Guid? GetStoreId(string storeName, Guid? techStoreId, Guid? homeStoreId)
    {
        return storeName.Contains("Tech", StringComparison.OrdinalIgnoreCase) ? techStoreId : homeStoreId;
    }

    private static Guid? FindBestCategory(string categoryName, List<CategoryDto> categories, Dictionary<string, string>? mapping = null)
    {
        // Priority 1: Exact mapping from category-mapping.json
        if (mapping != null)
        {
            if (TryResolveFromMapping(categoryName, categories, mapping, out var mappedId))
                return mappedId;

            // Try mapping each breadcrumb segment
            if (categoryName.Contains('>'))
            {
                foreach (var segment in categoryName.Split('>').Select(s => s.Trim()))
                {
                    if (TryResolveFromMapping(segment, categories, mapping, out var segId))
                        return segId;
                }
            }
        }

        // Priority 2: Exact name match (case-insensitive)
        var exact = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact.Id;

        // Priority 3: Breadcrumb path — try last segment first (most specific)
        if (categoryName.Contains('>'))
        {
            var segments = categoryName.Split('>').Select(s => s.Trim()).Reverse();
            foreach (var segment in segments)
            {
                var match = categories.FirstOrDefault(c => c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.Id;
            }
        }

        // Priority 4: Partial match (contains)
        var partial = categories.FirstOrDefault(c =>
            c.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase) ||
            categoryName.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
        if (partial != null) return partial.Id;

        // Priority 5: First breadcrumb segment fallback
        if (categoryName.Contains('>'))
        {
            var first = categoryName.Split('>')[0].Trim();
            var firstMatch = categories.FirstOrDefault(c => c.Name.Equals(first, StringComparison.OrdinalIgnoreCase));
            if (firstMatch != null) return firstMatch.Id;
        }

        return null;
    }

    private static bool TryResolveFromMapping(
        string name,
        List<CategoryDto> categories,
        Dictionary<string, string> mapping,
        out Guid? categoryId)
    {
        categoryId = null;
        if (!mapping.TryGetValue(name, out var mappedName))
            return false;

        var match = categories.FirstOrDefault(c => c.Name.Equals(mappedName, StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;

        categoryId = match.Id;
        return true;
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
            catch { }
            retries++;
            await Task.Delay(2000, ct);
        }
        throw new Exception("API Gateway did not become responsive in time.");
    }

    private async Task<T> LoadJsonAsync<T>(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Seed data file not found: {path}");
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream) ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
}
