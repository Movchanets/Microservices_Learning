using System.Net.Http.Headers;
using System.Security.Claims;
using ApiGateway.Contracts;

namespace ApiGateway.Services;

/// <summary>
/// BFF aggregation service: fetches orders from order-api, enriches with product details from catalog-api.
/// Mirrors the CartBffService pattern.
/// </summary>
public sealed class OrderBffService(
    IHttpClientFactory httpClientFactory,
    ILogger<OrderBffService> logger)
{
    /// <summary>
    /// Returns enriched orders for the given buyer. Combines order data with product metadata.
    /// </summary>
    public async Task<List<OrderBffDto>> GetOrdersByBuyerAsync(
        string buyerId,
        string? bearerToken,
        CancellationToken ct = default)
    {
        // 1. Get raw orders from order-api
        var orderClient = httpClientFactory.CreateClient("ordering-api");
        if (!string.IsNullOrEmpty(bearerToken))
            orderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var rawOrders = await orderClient.GetFromJsonAsync<List<RawOrderDto>>(
            $"/api/orders/buyer/{buyerId}", ct);

        if (rawOrders is null || rawOrders.Count == 0)
            return [];

        // 2. Collect all unique product IDs across all orders
        var productIds = rawOrders
            .SelectMany(o => o.Items)
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
            // Non-fatal: orders still work without product details, just shows fallback
            logger.LogWarning(ex, "Failed to fetch product details from catalog");
        }

        // 4. Merge orders with product details
        return rawOrders.Select(order => new OrderBffDto(
            order.Id,
            order.BuyerId,
            MapOrderStatus(order.Status),
            order.TotalAmount,
            order.CreatedAt,
            order.CompletedAt,
            order.Items.Select(item =>
            {
                productLookup.TryGetValue(item.ProductId, out var product);
                return new OrderItemBffDto(
                    item.Id,
                    item.ProductId,
                    product?.Name ?? item.ProductName,
                    product?.ImageUrl,
                    item.UnitPrice,
                    item.Quantity,
                    item.TotalPrice);
            }).ToList()
        )).ToList();
    }

    /// <summary>
    /// Returns a single enriched order by ID.
    /// </summary>
    public async Task<OrderBffDto?> GetOrderByIdAsync(
        Guid orderId,
        string? bearerToken,
        CancellationToken ct = default)
    {
        var orderClient = httpClientFactory.CreateClient("ordering-api");
        if (!string.IsNullOrEmpty(bearerToken))
            orderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        RawOrderDto? rawOrder;
        try
        {
            rawOrder = await orderClient.GetFromJsonAsync<RawOrderDto>(
                $"/api/orders/{orderId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (rawOrder is null)
            return null;

        var productIds = rawOrder.Items.Select(i => i.ProductId).Distinct().ToList();

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
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch product details from catalog");
        }

        return new OrderBffDto(
            rawOrder.Id,
            rawOrder.BuyerId,
            MapOrderStatus(rawOrder.Status),
            rawOrder.TotalAmount,
            rawOrder.CreatedAt,
            rawOrder.CompletedAt,
            rawOrder.Items.Select(item =>
            {
                productLookup.TryGetValue(item.ProductId, out var product);
                return new OrderItemBffDto(
                    item.Id,
                    item.ProductId,
                    product?.Name ?? item.ProductName,
                    product?.ImageUrl,
                    item.UnitPrice,
                    item.Quantity,
                    item.TotalPrice);
            }).ToList());
    }

    /// <summary>
    /// Maps the integer order status from the API to the string enum name
    /// expected by the Angular frontend (OrderStatus type).
    /// </summary>
    private static string MapOrderStatus(int status) => status switch
    {
        0 => "Submitted",
        1 => "InventoryReserved",
        2 => "PaymentProcessing",
        3 => "Completed",
        4 => "Cancelled",
        5 => "Faulted",
        6 => "Processing",
        7 => "Shipped",
        8 => "Delivered",
        _ => "Unknown"
    };
}
