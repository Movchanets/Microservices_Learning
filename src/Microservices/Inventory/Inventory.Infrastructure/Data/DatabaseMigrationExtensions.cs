using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Infrastructure.Database;
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
        => app.ApplyMigrations<InventoryDbContext>("Inventory");


    /// <summary>
    /// Seeds inventory stock for development.
    /// Inventory items are created with 0 stock by ProductCreatedConsumer when Catalog
    /// products are seeded. This method adds stock quantities to those items.
    /// Runs as a background task with retries to handle the race condition where
    /// ProductCreatedConsumer hasn't processed all catalog events yet on startup.
    /// </summary>
    public static async Task SeedDataAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inventory.DatabaseSeeding");

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

        // Retry for up to 30 seconds — ProductCreatedConsumer needs time to
        // process catalog events and create inventory items with correct ProductIds.
        const int maxAttempts = 15;
        const int delayMs = 2000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            // Create a fresh scope+context each attempt so we see newly-created items
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var items = context.InventoryItems.ToList();
            var updated = 0;
            var missing = 0;

            foreach (var (sku, quantity) in stockMap)
            {
                var item = items.FirstOrDefault(i => i.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    missing++;
                    continue;
                }

                if (item.AvailableQuantity == 0)
                {
                    item.AddStock(quantity);
                    updated++;
                    logger.LogInformation("Stocked SKU {Sku} with {Quantity} units.", item.Sku, quantity);
                }
            }

            if (updated > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Inventory seed: {UpdatedCount} items stocked.", updated);
            }

            if (missing == 0)
            {
                logger.LogInformation("All inventory items present and stocked.");
                return;
            }

            if (attempt < maxAttempts)
            {
                logger.LogInformation(
                    "Inventory seed: {MissingCount} SKUs not yet created by ProductCreatedConsumer. " +
                    "Retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})...",
                    missing, delayMs, attempt, maxAttempts);
                await Task.Delay(delayMs);
            }
        }

        logger.LogWarning("Inventory seed: some SKUs were not created after {MaxAttempts} attempts. Seed incomplete.", maxAttempts);
    }
}
