using System.Text.Json;
using Cart.Domain.Aggregates;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Cart.Infrastructure.Repositories;

public class CartRepository(IDistributedCache cache, CartDbContext dbContext) : ICartRepository
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

    public async Task<ShoppingCart> GetCartAsync(string buyerId, CancellationToken ct = default)
    {
        // Try getting from cache first
        var data = await cache.GetStringAsync(buyerId, ct);
        if (!string.IsNullOrEmpty(data))
        {
            return JsonSerializer.Deserialize<ShoppingCart>(data, JsonOptions) ?? new ShoppingCart(buyerId);
        }

        // If not in cache, load from database
        var cart = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, ct);

        if (cart == null)
        {
            cart = new ShoppingCart(buyerId);
        }
        else
        {
            // Set it in cache for future reads
            await UpdateCacheAsync(cart, ct);
        }

        return cart;
    }

    public async Task<ShoppingCart> UpdateCartAsync(ShoppingCart cart, CancellationToken ct = default)
    {
        var existing = await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == cart.BuyerId, ct);

        if (existing == null)
        {
            dbContext.ShoppingCarts.Add(cart);
        }
        else
        {
            var existingItems = existing.Items.ToDictionary(i => i.Sku);
            var newItems = cart.Items.ToDictionary(i => i.Sku);

            // Remove items no longer in cart
            foreach (var existingItem in existingItems.Values)
            {
                if (!newItems.ContainsKey(existingItem.Sku))
                    dbContext.CartItems.Remove(existingItem);
            }

            // Update existing items or add new ones
            foreach (var newItem in newItems.Values)
            {
                if (existingItems.TryGetValue(newItem.Sku, out var existingItem))
                {
                    existingItem.SetQuantity(newItem.Quantity);
                    existingItem.SetPrice(newItem.Price);
                }
                else
                {
                    existing.AddItem(newItem.Sku, newItem.Quantity, newItem.Price);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
        await UpdateCacheAsync(existing ?? cart, ct);
        return existing ?? cart;
    }

    public async Task DeleteCartAsync(string buyerId, CancellationToken ct = default)
    {
        var cart = await dbContext.ShoppingCarts.FindAsync([buyerId], ct);
        if (cart != null)
        {
            dbContext.ShoppingCarts.Remove(cart);
            await dbContext.SaveChangesAsync(ct);
        }

        await cache.RemoveAsync(buyerId, ct);
    }

    public void Add(ShoppingCart item)
    {
        dbContext.ShoppingCarts.Add(item);
    }

    public void Update(ShoppingCart item)
    {
        dbContext.ShoppingCarts.Update(item);
    }

    public void Remove(ShoppingCart item)
    {
        dbContext.ShoppingCarts.Remove(item);
    }

    private async Task UpdateCacheAsync(ShoppingCart cart, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromDays(7)
        };
        var data = JsonSerializer.Serialize(cart, JsonOptions);
        await cache.SetStringAsync(cart.BuyerId, data, options, ct);
    }
}