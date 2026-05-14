using System.Text.Json;
using Cart.Domain.Aggregates;
using Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Cart.Infrastructure.Repositories;

public class CartRepository(IDistributedCache cache, CartDbContext dbContext) : ICartRepository
{
    public async Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<ShoppingCart> GetCartAsync(string buyerId, CancellationToken ct = default)
    {
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        // Try getting from cache first
        var data = await cache.GetStringAsync(buyerId, ct);
        if (!string.IsNullOrEmpty(data))
        {
            return JsonSerializer.Deserialize<ShoppingCart>(data, jsonOptions) ?? new ShoppingCart(buyerId);
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
            // We clear and re-add instead of complex syncing for simplicity
            // Alternatively, domain logic already updated `cart.Items`
            // Since `cart` here is the aggregate root that we might have attached,
            // EF will track changes. If it's detached, we just sync items.
            dbContext.Entry(existing).CurrentValues.SetValues(cart);
            // Sync items (EF Core handles collections if configured properly, but usually manual sync is needed)
            existing.Clear();
            foreach (var item in cart.Items)
            {
                existing.AddItem(item.Sku, item.Quantity);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        await UpdateCacheAsync(cart, ct);

        return cart;
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
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        var data = JsonSerializer.Serialize(cart, jsonOptions);
        await cache.SetStringAsync(cart.BuyerId, data, options, ct);
    }
}