using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Seeder.App.Models;

namespace Seeder.App.Seeders;

/// <summary>
/// End-to-end order flow seeder: adds items to cart, checks out,
/// polls for order completion, and logs the final status.
/// Runs after all product/inventory seeding is complete.
/// </summary>
public class OrderFlowSeeder
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public OrderFlowSeeder(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Registers the buyer user if not already present, returns auth token.
    /// </summary>
    public async Task<(string Token, Guid BuyerId)> EnsureBuyerAsync(
        UserModel buyer, CancellationToken ct)
    {
        // Try login first
        var loginResp = await _client.PostAsJsonAsync(
            "/api/identity/auth/login",
            new { buyer.Email, buyer.Password }, ct);

        if (!loginResp.IsSuccessStatusCode)
        {
            // Register
            _logger.LogInformation("Registering buyer {Email}...", buyer.Email);
            var regResp = await _client.PostAsJsonAsync(
                "/api/identity/auth/register", buyer, ct);
            regResp.EnsureSuccessStatusCode();
        }

        var token = await LoginAsync(buyer.Email, buyer.Password, ct);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Fetch buyer ID from /api/identity/users (need admin token for that,
        // but /bff/cart returns buyerId in the response — use that instead)
        // Simpler: call /api/cart which returns buyerId if authenticated
        var cartResp = await _client.GetAsync("/api/cart", ct);
        if (cartResp.IsSuccessStatusCode)
        {
            var cart = await cartResp.Content.ReadFromJsonAsync<CartResponse>(JsonOpts, ct);
            if (cart?.BuyerId != null)
                return (token, cart.BuyerId.Value);
        }

        // Fallback: parse from JWT claims (sub claim)
        var buyerId = GetBuyerIdFromToken(token);
        return (token, buyerId);
    }

    /// <summary>
    /// Adds a few products to the cart, checks out, polls for status.
    /// </summary>
    public async Task RunOrderFlowAsync(
        string token,
        Dictionary<string, (Guid StoreId, Guid ProductId, Guid SkuId)> productIds,
        CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // ── Step 1: Add items to cart ──────────────────────────
        var itemsToAdd = new[]
        {
            ("PHONE-IPHONE-15-PRO", 1),   // iPhone 15 Pro × 1
            ("AUDIO-SONY-WH1000XM5", 2),  // Sony headphones × 2
            ("BOOK-CLEANCODE", 3),         // Clean Code × 3
        };

        _logger.LogInformation("═══ Order Flow: Adding items to cart ═══");

        foreach (var (sku, qty) in itemsToAdd)
        {
            if (!productIds.TryGetValue(sku, out var ids))
            {
                _logger.LogWarning("Product {Sku} not found in seeded products, skipping.", sku);
                continue;
            }

            var addResp = await _client.PostAsJsonAsync("/api/cart/items",
                new { ProductId = ids.ProductId, SkuId = ids.SkuId, SkuCode = sku, Quantity = qty }, ct);

            if (addResp.IsSuccessStatusCode)
            {
                _logger.LogInformation("  ✓ Added {Sku} × {Qty}", sku, qty);
            }
            else
            {
                var err = await addResp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("  ✗ Failed to add {Sku}: {StatusCode} - {Error}",
                    sku, addResp.StatusCode, err);
            }
        }

        // ── Step 2: Verify cart contents ───────────────────────
        _logger.LogInformation("═══ Order Flow: Verifying cart ═══");
        var cartResp = await _client.GetAsync("/api/cart", ct);
        if (cartResp.IsSuccessStatusCode)
        {
            var cart = await cartResp.Content.ReadFromJsonAsync<CartResponse>(JsonOpts, ct);
            _logger.LogInformation("  Cart has {Count} items, total: ${Total:F2}",
                cart?.Items?.Count ?? 0,
                cart?.Items?.Sum(i => i.Price * i.Quantity) ?? 0);
        }

        // ── Step 3: Checkout ───────────────────────────────────
        _logger.LogInformation("═══ Order Flow: Checking out ═══");
        var checkoutResp = await _client.PostAsJsonAsync("/api/cart/checkout",
            new
            {
                AddressLine1 = "123 Seeder St",
                City = "Portland",
                State = "OR",
                PostalCode = "97201",
                Country = "US"
            }, ct);

        if (!checkoutResp.IsSuccessStatusCode)
        {
            var err = await checkoutResp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Checkout failed: {StatusCode} - {Error}",
                checkoutResp.StatusCode, err);
            return;
        }

        var checkoutResult = await checkoutResp.Content
            .ReadFromJsonAsync<CheckoutResponse>(JsonOpts, ct);
        var correlationId = checkoutResult?.CorrelationId;
        _logger.LogInformation("  ✓ Checkout accepted. CorrelationId: {CorrelationId}", correlationId);

        // ── Step 4: Poll for order status ──────────────────────
        _logger.LogInformation("═══ Order Flow: Polling for order status ═══");
        await PollOrderStatusAsync(token, correlationId, ct);
    }

    private async Task PollOrderStatusAsync(
        string token, Guid? correlationId, CancellationToken ct)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // We don't know the buyerId in this method, so list orders by
        // fetching the buyer's orders. But we need buyerId for that endpoint.
        // Instead, just try GET /api/orders/{correlationId} directly.
        if (correlationId == null)
        {
            _logger.LogWarning("No correlation ID — cannot poll for order.");
            return;
        }

        var orderId = correlationId.Value;
        const int maxAttempts = 30;          // 30 × 2s = 60s max
        const int delayMs = 2000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var resp = await _client.GetAsync($"/api/orders/{orderId}", ct);
                if (resp.IsSuccessStatusCode)
                {
                    var order = await resp.Content
                        .ReadFromJsonAsync<OrderResponse>(JsonOpts, ct);

                    _logger.LogInformation("  [{Attempt}/{Max}] Order {Id} — Status: {Status}",
                        attempt, maxAttempts, orderId, order?.StatusName ?? "Unknown");

                    if (order?.StatusName is "Completed" or "Cancelled" or "Faulted")
                    {
                        _logger.LogInformation(
                            "═══ Order Flow: Final status = {Status} ═══", order.StatusName);
                        LogOrderSummary(order);
                        return;
                    }
                }
                else if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation(
                        "  [{Attempt}/{Max}] Order not yet created (404), waiting...",
                        attempt, maxAttempts);
                }
                else
                {
                    var err = await resp.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "  [{Attempt}/{Max}] Unexpected {StatusCode}: {Error}",
                        attempt, maxAttempts, resp.StatusCode, err);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "  [{Attempt}/{Max}] Error polling order status",
                    attempt, maxAttempts);
            }

            await Task.Delay(delayMs, ct);
        }

        _logger.LogWarning(
            "Order {OrderId} did not reach a terminal state within {Seconds}s.",
            orderId, maxAttempts * delayMs / 1000);
    }

    private void LogOrderSummary(OrderResponse order)
    {
        _logger.LogInformation("  ┌─ Order Summary ─────────────────────────");
        _logger.LogInformation("  │ Order ID:  {Id}", order.Id);
        _logger.LogInformation("  │ Buyer ID:  {BuyerId}", order.BuyerId);
        _logger.LogInformation("  │ Status:    {Status}", order.StatusName);
        _logger.LogInformation("  │ Total:     ${Total:F2}", order.TotalAmount);
        _logger.LogInformation("  │ Created:   {Created}", order.CreatedAt);
        if (order.CompletedAt != null)
            _logger.LogInformation("  │ Completed: {Completed}", order.CompletedAt);
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                _logger.LogInformation("  │  → {Name} × {Qty} @ ${Price:F2}",
                    item.ProductName ?? item.Sku, item.Quantity, item.UnitPrice);
            }
        }
        _logger.LogInformation("  └─────────────────────────────────────────");
    }

    private async Task<string> LoginAsync(string email, string password, CancellationToken ct)
    {
        var resp = await _client.PostAsJsonAsync(
            "/api/identity/auth/login",
            new { Email = email, Password = password }, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts, ct);
        return result!.AccessToken;
    }

    private static Guid GetBuyerIdFromToken(string jwt)
    {
        // Decode JWT payload (base64url) to extract "sub" claim
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new InvalidOperationException("Invalid JWT format");

        var payload = parts[1];
        // Pad for base64
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        using var doc = JsonDocument.Parse(json);

        // Try "sub" or ClaimTypes.NameIdentifier URI
        if (doc.RootElement.TryGetProperty("sub", out var sub))
            return Guid.Parse(sub.GetString()!);

        var nameId = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        if (doc.RootElement.TryGetProperty(nameId, out var nameIdProp))
            return Guid.Parse(nameIdProp.GetString()!);

        throw new InvalidOperationException("Could not extract buyer ID from JWT");
    }

    // ── DTOs ───────────────────────────────────────────────

    private record CartResponse(Guid? BuyerId, Guid? CartId, List<CartItemDto> Items);
    private record CartItemDto(Guid ProductId, string Sku, decimal Price, int Quantity);
    private record CheckoutResponse(Guid CorrelationId);

    private record OrderResponse(
        Guid Id,
        string BuyerId,
        int Status,
        decimal TotalAmount,
        string CreatedAt,
        string? CompletedAt,
        List<OrderItemResponse>? Items)
    {
        public string StatusName => Status switch
        {
            0 => "Submitted",
            1 => "InventoryReserved",
            2 => "PaymentProcessing",
            3 => "Completed",
            4 => "Cancelled",
            5 => "Faulted",
            _ => $"Unknown({Status})"
        };
    }

    private record OrderItemResponse(
        Guid Id,
        Guid ProductId,
        string? ProductName,
        string? Sku,
        decimal UnitPrice,
        int Quantity,
        decimal TotalPrice);
}
