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

        var clothingCategory = Category.Create(
            name: "Clothing",
            description: "Apparel and accessories",
            sortOrder: 3);

        var sportsCategory = Category.Create(
            name: "Sports & Outdoors",
            description: "Fitness gear and outdoor equipment",
            sortOrder: 4);

        var booksCategory = Category.Create(
            name: "Books",
            description: "Fiction, non-fiction, and technical books",
            sortOrder: 5);

        context.Categories.AddRange(electronicsCategory, homeCategory, clothingCategory, sportsCategory, booksCategory);

        // Must match Identity dev seeder seller IDs.
        var sellerTechStoreId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sellerHomeStoreId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var products = new[]
        {
            // ── Electronics (Tech Store) ──────────────────────────────────
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
                name: "Samsung 65\" OLED 4K TV",
                description: "Stunning OLED display with smart TV features.",
                price: 1799m,
                currency: "USD",
                sku: "TV-SAMSUNG-OLED-65",
                categoryId: electronicsCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["tv", "oled", "samsung", "4k"],
                imageUrl: "https://picsum.photos/seed/samsungtv/1200/800"),
            Product.Create(
                name: "Logitech MX Master 3S",
                description: "Ergonomic wireless mouse with MagSpeed scroll.",
                price: 99m,
                currency: "USD",
                sku: "PERIPH-LOGI-MXM3S",
                categoryId: electronicsCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["mouse", "wireless", "ergonomic"],
                imageUrl: "https://picsum.photos/seed/logimx/1200/800"),

            // ── Home & Kitchen (Home Store) ───────────────────────────────
            Product.Create(
                name: "Ninja Air Fryer",
                description: "Digital air fryer for fast and healthy cooking.",
                price: 149m,
                currency: "USD",
                sku: "HOME-NINJA-AIR-FRYER",
                categoryId: homeCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["kitchen", "air-fryer", "appliance"],
                imageUrl: "https://picsum.photos/seed/ninjaairfryer/1200/800"),
            Product.Create(
                name: "Dyson V15 Detect",
                description: "Cordless vacuum with laser dust detection.",
                price: 749m,
                currency: "USD",
                sku: "HOME-DYSON-V15",
                categoryId: homeCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["vacuum", "cordless", "dyson"],
                imageUrl: "https://picsum.photos/seed/dysonv15/1200/800"),
            Product.Create(
                name: "Le Creuset Dutch Oven",
                description: "Enameled cast iron 5.5-qt Dutch oven.",
                price: 399m,
                currency: "USD",
                sku: "HOME-LECREUSET-DO55",
                categoryId: homeCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["cookware", "cast-iron", "le-creuset"],
                imageUrl: "https://picsum.photos/seed/lecreuset/1200/800"),

            // ── Clothing (Tech Store — cross-sell) ────────────────────────
            Product.Create(
                name: "Nike Air Max 270",
                description: "Lightweight sneakers with Max Air unit.",
                price: 150m,
                currency: "USD",
                sku: "SHOE-NIKE-AMAX270",
                categoryId: clothingCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["shoes", "nike", "sneakers"],
                imageUrl: "https://picsum.photos/seed/nikeairmax/1200/800"),
            Product.Create(
                name: "Levi's 501 Original Jeans",
                description: "Classic straight-fit jeans in medium wash.",
                price: 69m,
                currency: "USD",
                sku: "PANTS-LEVIS-501",
                categoryId: clothingCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["jeans", "levi", "denim"],
                imageUrl: "https://picsum.photos/seed/levis501/1200/800"),

            // ── Sports & Outdoors (Home Store) ────────────────────────────
            Product.Create(
                name: "Yoga Mat Premium",
                description: "6mm thick non-slip yoga mat with carrying strap.",
                price: 45m,
                currency: "USD",
                sku: "SPORT-YOGAMAT-6MM",
                categoryId: sportsCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["yoga", "fitness", "mat"],
                imageUrl: "https://picsum.photos/seed/yogamat/1200/800"),
            Product.Create(
                name: "Hydro Flask 32oz",
                description: "Insulated stainless steel water bottle.",
                price: 44m,
                currency: "USD",
                sku: "SPORT-HYDROFLASK-32",
                categoryId: sportsCategory.Id,
                sellerId: sellerHomeStoreId,
                tags: ["water-bottle", "insulated", "outdoor"],
                imageUrl: "https://picsum.photos/seed/hydroflask/1200/800"),

            // ── Books (Tech Store) ────────────────────────────────────────
            Product.Create(
                name: "Clean Code",
                description: "A handbook of agile software craftsmanship by Robert C. Martin.",
                price: 39m,
                currency: "USD",
                sku: "BOOK-CLEANCODE",
                categoryId: booksCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["programming", "software", "craftsmanship"],
                imageUrl: "https://picsum.photos/seed/cleancode/1200/800"),
            Product.Create(
                name: "Designing Data-Intensive Applications",
                description: "The big ideas behind reliable, scalable systems.",
                price: 42m,
                currency: "USD",
                sku: "BOOK-DDIA",
                categoryId: booksCategory.Id,
                sellerId: sellerTechStoreId,
                tags: ["distributed-systems", "architecture", "databases"],
                imageUrl: "https://picsum.photos/seed/ddia/1200/800"),
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
