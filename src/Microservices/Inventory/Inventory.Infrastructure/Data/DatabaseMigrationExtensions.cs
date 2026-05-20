using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Inventory.Domain.Aggregates;

namespace Inventory.Infrastructure.Data;

/// <summary>
/// Provides startup migration helpers for the Inventory service.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Inventory DbContext.
    /// Useful for local development and integration tests.
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Inventory.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            logger.LogInformation("Applying Inventory database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Inventory database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying Inventory database migrations.");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Seeds inventory stock for development.
    /// Inventory items are created with 0 stock by ProductCreatedConsumer when Catalog
    /// products are seeded. This method adds stock quantities to those items.
    /// </summary>
    public static WebApplication SeedData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Inventory.DatabaseSeeding");

        // Stock quantities for each SKU — must match Catalog seeder products.
        var stockMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Electronics
            ["PHONE-IPHONE-15-PRO"] = 50,
            ["AUDIO-SONY-WH1000XM5"] = 100,
            ["TV-SAMSUNG-OLED-65"] = 25,
            ["PERIPH-LOGI-MXM3S"] = 200,
            // Home & Kitchen
            ["HOME-NINJA-AIR-FRYER"] = 75,
            ["HOME-DYSON-V15"] = 30,
            ["HOME-LECREUSET-DO55"] = 40,
            // Clothing
            ["SHOE-NIKE-AMAX270"] = 150,
            ["PANTS-LEVIS-501"] = 120,
            // Sports & Outdoors
            ["SPORT-YOGAMAT-6MM"] = 300,
            ["SPORT-HYDROFLASK-32"] = 180,
            // Books
            ["BOOK-CLEANCODE"] = 500,
            ["BOOK-DDIA"] = 400,
        };

        var items = context.InventoryItems.ToList();
        var updated = 0;
        var created = 0;

        foreach (var (sku, quantity) in stockMap)
        {
            var item = items.FirstOrDefault(i => i.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                // Item not yet created by ProductCreatedConsumer (race condition) — create it now
                item = InventoryItem.Create(sku, quantity);
                context.InventoryItems.Add(item);
                created++;
                logger.LogInformation("Created inventory for SKU {Sku} with {Quantity} units.", sku, quantity);
            }
            else if (item.AvailableQuantity == 0)
            {
                item.AddStock(quantity);
                updated++;
                logger.LogInformation("Stocked SKU {Sku} with {Quantity} units.", item.Sku, quantity);
            }
        }

        if (created > 0 || updated > 0)
        {
            context.SaveChangesAsync().GetAwaiter().GetResult();
            logger.LogInformation("Inventory seed: {CreatedCount} created, {UpdatedCount} stocked.", created, updated);
        }
        else
        {
            logger.LogInformation("Inventory already stocked or no items to stock. Skipping.");
        }

        return app;
    }
}
