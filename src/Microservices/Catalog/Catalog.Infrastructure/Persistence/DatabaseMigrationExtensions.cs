using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Provides startup migration helpers for the Catalog service.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Catalog DbContext.
    /// Useful for local development and integration tests.
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Catalog.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            logger.LogInformation("Applying Catalog database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Catalog database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying Catalog database migrations.");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Seeds development data for Catalog to validate search indexing flow.
    /// </summary>
    public static WebApplication SeedData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Catalog.DatabaseSeeding");

        if (context.Products.Any())
        {
            // Products may already exist from a previous startup when Search consumer queues were not bound yet.
            // Touching products emits ProductUpdated domain events to replay indexing messages.
            var existingProducts = context.Products
                .Where(p => p.Status != ProductStatus.Deleted)
                .ToList();

            foreach (var product in existingProducts)
            {
                product.Update(
                    product.Name,
                    product.Description,
                    product.CategoryId,
                    product.Tags,
                    product.ImageUrl);

                if (!product.IsActive)
                    product.Activate();
            }

            context.SaveChangesAsync().GetAwaiter().GetResult();
            logger.LogInformation("Catalog seed replayed {ProductCount} products to messaging pipeline.", existingProducts.Count);
            return app;
        }

        var electronicsCategory = Category.Create(
            name: "Electronics",
            description: "Devices and gadgets",
            sortOrder: 1);

        var homeCategory = Category.Create(
            name: "Home & Kitchen",
            description: "Home essentials and kitchen tools",
            sortOrder: 2);

        context.Categories.AddRange(electronicsCategory, homeCategory);

        // Must match Identity dev seeder seller IDs.
        var sellerTechStoreId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sellerHomeStoreId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var products = new[]
        {
            Product.Create(
                name: "iPhone 15 Pro",
                description: "Apple smartphone with advanced camera system.",
                price: 1199m,
                currency: "USD",
                sku: "PHONE-IPHONE-15-PRO",
                categoryId: electronicsCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["smartphone", "apple", "ios"],
                imageUrl: "https://picsum.photos/seed/iphone15/1200/800"),
            Product.Create(
                name: "Sony WH-1000XM5",
                description: "Wireless noise-canceling over-ear headphones.",
                price: 399m,
                currency: "USD",
                sku: "AUDIO-SONY-WH1000XM5",
                categoryId: electronicsCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["audio", "headphones", "wireless"],
                imageUrl: "https://picsum.photos/seed/sonyxm5/1200/800"),
            Product.Create(
                name: "Ninja Air Fryer",
                description: "Digital air fryer for fast and healthy cooking.",
                price: 149m,
                currency: "USD",
                sku: "HOME-NINJA-AIR-FRYER",
                categoryId: homeCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["kitchen", "air-fryer", "appliance"],
                imageUrl: "https://picsum.photos/seed/ninjaairfryer/1200/800")
        };

        foreach (var product in products)
        {
            product.Activate();
        }

        context.Products.AddRange(products);
        context.SaveChangesAsync().GetAwaiter().GetResult();

        logger.LogInformation("Catalog seed completed: {CategoryCount} categories and {ProductCount} products.", 2, products.Length);

        return app;
    }
}
