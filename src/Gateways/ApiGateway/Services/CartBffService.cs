using System.Net.Http.Headers;
using System.Security.Claims;
using ApiGateway.Contracts;

namespace ApiGateway.Services;

/// <summary>
/// BFF aggregation service: fetches cart from cart-api, enriches with product details from catalog-api.
/// </summary>
public sealed class CartBffService(
    IHttpClientFactory httpClientFactory,
    ILogger<CartBffService> logger)
{
    /// <summary>
    /// Returns enriched cart for the given buyer. Combines cart data with product metadata.
    /// </summary>
    public async Task<CartDto> GetCartWithDetailsAsync(
        string? buyerId,
        string? cartIdHeader,
        string? bearerToken,
        CancellationToken ct = default)
    {
        // 1. Get raw cart from cart-api
        var cartClient = httpClientFactory.CreateClient("cart-api");
        if (!string.IsNullOrEmpty(bearerToken))
            cartClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        if (!string.IsNullOrEmpty(cartIdHeader))
            cartClient.DefaultRequestHeaders.Add("X-Cart-Id", cartIdHeader);

        var rawCart = await cartClient.GetFromJsonAsync<RawCartResponse>(
            "/api/cart", ct);

        if (rawCart is null || rawCart.Items.Count == 0)
            return new CartDto(null, Guid.Empty, [], 0, 0);

        // 2. Collect unique product IDs
        var productIds = rawCart.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        // 3. Bulk-fetch product details from catalog-api
        Dictionary<Guid, ProductSummary> productLookup = new();
        try
        {
            var catalogClient = httpClientFactory.CreateClient("catalog-api");
            var response = await catalogClient.PostAsJsonAsync(
                "/api/catalog/products/by-ids", productIds, ct);

            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<List<ProductSummary>>(cancellationToken: ct);
                if (products is not null)
                    productLookup = products.ToDictionary(p => p.Id);
            }
            else
            {
                logger.LogWarning("Catalog by-ids returned {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Non-fatal: cart still works without product details, just shows fallback
            logger.LogWarning(ex, "Failed to fetch product details from catalog");
        }

        // 4. Merge cart items with product details
        var itemDtos = rawCart.Items.Select(item =>
        {
            productLookup.TryGetValue(item.ProductId, out var product);

            return new CartItemDetailsDto(
                item.ProductId,
                item.SkuId,
                item.SkuCode,
                product?.Name ?? "Unknown Product",
                product?.ImageUrl,
                item.Quantity,
                item.Price,
                item.Quantity * item.Price,
                item.StoreId);
        }).ToList();

        return new CartDto(
            rawCart.BuyerId,
            rawCart.CartId,
            itemDtos,
            itemDtos.Sum(x => x.LineTotal),
            itemDtos.Sum(x => x.Quantity));
    }
}
