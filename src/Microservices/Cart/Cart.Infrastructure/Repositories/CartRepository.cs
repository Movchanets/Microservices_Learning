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
    /// When both buyerId and cartId are present, merges anonymous cart into authenticated cart.
    /// </summary>
    public async Task<ShoppingCart> GetCartAsync(Guid? buyerId, Guid? cartId = null, CancellationToken ct = default)
    {
        // Authenticated user with anonymous cart hint → merge
        if (buyerId.HasValue && cartId.HasValue)
        {
            var merged = await MergeAnonymousIntoAuthenticatedAsync(buyerId.Value, cartId.Value, ct);
            if (merged is not null)
            {
                await UpdateCacheAsync(merged, ct);
                return merged;
            }
        }

        var cacheKey = ResolveCacheKey(buyerId, cartId);

        if (cacheKey is not null)
        {
            var data = await cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrEmpty(data))
                return JsonSerializer.Deserialize<ShoppingCart>(data, JsonOptions) ?? new ShoppingCart(buyerId);
        }

        var cart = await FindCartAsync(buyerId, cartId, ct);

        if (cart is not null)
            await UpdateCacheAsync(cart, ct);
        else
            cart = new ShoppingCart(buyerId);

        return cart;
    }

    /// <summary>
    /// Write path: loads tracked cart from DB. Creates if missing.
    /// When both buyerId and cartId are present, merges anonymous cart into authenticated cart.
    /// </summary>
    public async Task<ShoppingCart> GetOrCreateTrackedCartAsync(Guid? buyerId, Guid? cartId = null, CancellationToken ct = default)
    {
        // Authenticated user with anonymous cart hint → merge
        if (buyerId.HasValue && cartId.HasValue)
        {
            var merged = await MergeAnonymousIntoAuthenticatedTrackedAsync(buyerId.Value, cartId.Value, ct);
            if (merged is not null)
                return merged;
        }

        var cart = await FindCartAsync(buyerId, cartId, ct);

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
    /// we catch the DbUpdateConcurrencyException, immediately evict the cache entry
    /// to prevent database-to-cache desynchronization, and re-throw the exception.
    /// </summary>
    public async Task SaveCartAsync(ShoppingCart cart, CancellationToken ct = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
            await UpdateCacheAsync(cart, ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict detected when saving cart {CartId}. Evicting cache and throwing.", cart.Id);

            var cacheKey = ResolveCacheKey(cart.BuyerId, cart.Id);
            if (cacheKey is not null)
                await cache.RemoveAsync(cacheKey, ct);

            throw;
        }
    }

    public async Task DeleteCartAsync(Guid? buyerId, Guid? cartId = null, CancellationToken ct = default)
    {
        var cart = await FindCartAsync(buyerId, cartId, ct);

        if (cart is not null)
        {
            dbContext.ShoppingCarts.Remove(cart);
            await dbContext.SaveChangesAsync(ct);
        }

        var cacheKey = ResolveCacheKey(buyerId, cartId);
        if (cacheKey is not null)
            await cache.RemoveAsync(cacheKey, ct);
    }

    public void Add(ShoppingCart item) => dbContext.ShoppingCarts.Add(item);

    public void Update(ShoppingCart item) => dbContext.ShoppingCarts.Update(item);

    public void Remove(ShoppingCart item) => dbContext.ShoppingCarts.Remove(item);

    /// <summary>
    /// Finds a cart by BuyerId (authenticated) or by cart Id (anonymous).
    /// </summary>
    private async Task<ShoppingCart?> FindCartAsync(Guid? buyerId, Guid? cartId, CancellationToken ct)
    {
        if (buyerId.HasValue)
            return await dbContext.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.BuyerId == buyerId.Value, ct);

        if (cartId.HasValue)
            return await dbContext.ShoppingCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId.Value, ct);

        return null;
    }

    /// <summary>
    /// Read-only merge: finds both carts (untracked), merges anonymous items into authenticated.
    /// Returns the merged cart (untracked) or null if nothing to merge.
    /// </summary>
    private async Task<ShoppingCart?> MergeAnonymousIntoAuthenticatedAsync(
        Guid buyerId, Guid anonCartId, CancellationToken ct)
    {
        var authCart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, ct);

        var anonCart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == anonCartId && c.BuyerId == null, ct);

        if (anonCart is null || anonCart.Items.Count == 0)
            return authCart; // nothing to merge

        // If auth cart doesn't exist yet, claim the anonymous cart
        if (authCart is null)
        {
            // Need tracked entity to update BuyerId — re-fetch tracked
            var trackedAnon = await dbContext.ShoppingCarts
                .Include(c => c.Items)
                .FirstAsync(c => c.Id == anonCartId, ct);
            trackedAnon.Claim(buyerId);
            await dbContext.SaveChangesAsync(ct);

            // Invalidate anon cache
            await cache.RemoveAsync($"anon:{anonCartId}", ct);
            return trackedAnon;
        }

        // Both exist — merge anonymous items into auth cart using tracked entities
        var trackedAuth = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstAsync(c => c.BuyerId == buyerId, ct);

        var trackedAnonDelete = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == anonCartId, ct);

        MergeItems(trackedAuth, trackedAnonDelete);
        dbContext.ShoppingCarts.Remove(trackedAnonDelete);
        await dbContext.SaveChangesAsync(ct);

        // Invalidate both caches
        await cache.RemoveAsync($"anon:{anonCartId}", ct);
        await cache.RemoveAsync(buyerId.ToString(), ct);

        return trackedAuth;
    }

    /// <summary>
    /// Tracked merge: same as above but returns tracked entity for further mutations.
    /// </summary>
    private async Task<ShoppingCart?> MergeAnonymousIntoAuthenticatedTrackedAsync(
        Guid buyerId, Guid anonCartId, CancellationToken ct)
    {
        var authCart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, ct);

        var anonCart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == anonCartId && c.BuyerId == null, ct);

        if (anonCart is null || anonCart.Items.Count == 0)
            return authCart; // nothing to merge

        // If auth cart doesn't exist yet, claim the anonymous cart
        if (authCart is null)
        {
            anonCart.Claim(buyerId);
            await dbContext.SaveChangesAsync(ct);
            await cache.RemoveAsync($"anon:{anonCartId}", ct);
            return anonCart;
        }

        // Both exist — merge
        MergeItems(authCart, anonCart);
        dbContext.ShoppingCarts.Remove(anonCart);
        await dbContext.SaveChangesAsync(ct);

        await cache.RemoveAsync($"anon:{anonCartId}", ct);
        await cache.RemoveAsync(buyerId.ToString(), ct);

        return authCart;
    }

    /// <summary>
    /// Merges items from source cart into target cart.
    /// Matching items (same ProductId) get quantity added; new items get moved.
    /// </summary>
    private static void MergeItems(ShoppingCart target, ShoppingCart source)
    {
        foreach (var sourceItem in source.Items.ToList())
        {
            var existing = target.Items.FirstOrDefault(i => i.MatchesProduct(sourceItem.ProductId));
            if (existing is not null)
            {
                existing.AddQuantity(sourceItem.Quantity);
            }
            else
            {
                // Move item to target cart
                target.AddItem(sourceItem.ProductId, sourceItem.Quantity, sourceItem.StoreId, sourceItem.Price);
            }
        }
    }

    /// <summary>
    /// Resolves cache key: buyerId string for authenticated, "anon:{cartId}" for anonymous.
    /// Returns null if neither is available.
    /// </summary>
    private static string? ResolveCacheKey(Guid? buyerId, Guid? cartId)
    {
        if (buyerId.HasValue)
            return buyerId.Value.ToString();

        if (cartId.HasValue)
            return $"anon:{cartId.Value}";

        return null;
    }

    private async Task UpdateCacheAsync(ShoppingCart cart, CancellationToken ct)
    {
        var cacheKey = ResolveCacheKey(cart.BuyerId, cart.Id);
        if (cacheKey is null) return;

        var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(7) };
        var data = JsonSerializer.Serialize(cart, JsonOptions);
        await cache.SetStringAsync(cacheKey, data, options, ct);
    }
}
