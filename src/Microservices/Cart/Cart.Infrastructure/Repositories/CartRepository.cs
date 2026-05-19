using System.Text.Json;
using Cart.Domain.Aggregates;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Cart.Infrastructure.Repositories;

public sealed class CartRepository(
    IDistributedCache cache,
    CartDbContext dbContext,
    ILogger<CartRepository> logger) : ICartRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new ShoppingCartJsonConverter() }
    };

    public async Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    /// <summary>
    /// Read-only: cache-first, untracked. For queries only.
    /// </summary>
    public async Task<ShoppingCart> GetCartAsync(string buyerId, CancellationToken ct = default)
    {
        var data = await cache.GetStringAsync(buyerId, ct);
        if (!string.IsNullOrEmpty(data))
            return JsonSerializer.Deserialize<ShoppingCart>(data, JsonOptions) ?? new ShoppingCart(buyerId);

        var cart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, ct);

        if (cart is not null)
            await UpdateCacheAsync(cart, ct);
        else
            cart = new ShoppingCart(buyerId);

        return cart;
    }

    /// <summary>
    /// Write path: loads tracked cart from DB. Creates if missing.
    /// Domain operations happen directly on the returned tracked entity.
  
    /// </summary>
    public async Task<ShoppingCart> GetOrCreateTrackedCartAsync(string buyerId, CancellationToken ct = default)
    {
        var cart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, ct);

        if (cart is null)
        {
            cart = new ShoppingCart(buyerId);
            dbContext.ShoppingCarts.Add(cart);
           
        }

        return cart;
    }

    /// <summary>
    /// Persists tracked changes and invalidates the cache.
    /// Handles optimistic concurrency via xmin: if another request modified the row,
    /// we clear the tracker, reload the cart, and retry.
    /// </summary>
    public async Task SaveCartAsync(ShoppingCart cart, CancellationToken ct = default)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(ct);
                await UpdateCacheAsync(cart, ct);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                dbContext.ChangeTracker.Clear();
                // Reload the cart so the caller's next operation has fresh state
                var reloaded = await dbContext.ShoppingCarts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.BuyerId == cart.BuyerId, ct);
                if (reloaded is not null)
                {
                    // Re-apply the caller's intended state onto the reloaded entity
                    // (the caller must re-invoke domain methods after this throws)
                    logger.LogWarning(
                        "Concurrency conflict on cart {BuyerId}, attempt {Attempt}/{Max}",
                        cart.BuyerId, attempt + 1, maxRetries);
                }
            }
        }

        // Final attempt — let exception propagate if all retries exhausted
        await dbContext.SaveChangesAsync(ct);
        await UpdateCacheAsync(cart, ct);
    }

    public async Task DeleteCartAsync(string buyerId, CancellationToken ct = default)
    {
        var cart = await dbContext.ShoppingCarts
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, ct);

        if (cart is not null)
        {
            dbContext.ShoppingCarts.Remove(cart);
            await dbContext.SaveChangesAsync(ct);
        }

        await cache.RemoveAsync(buyerId, ct);
    }

    public void Add(ShoppingCart item) => dbContext.ShoppingCarts.Add(item);

    public void Update(ShoppingCart item) => dbContext.ShoppingCarts.Update(item);

    public void Remove(ShoppingCart item) => dbContext.ShoppingCarts.Remove(item);

    private async Task UpdateCacheAsync(ShoppingCart cart, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(7) };
        var data = JsonSerializer.Serialize(cart, JsonOptions);
        await cache.SetStringAsync(cart.BuyerId, data, options, ct);
    }
}
