using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;

namespace StoreManagement.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StoreManagement.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            logger.LogInformation("Applying StoreManagement database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("StoreManagement database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying StoreManagement database migrations.");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Seeds initial stores for the dev environment.
    /// Must run after Identity seeding (depends on seller user IDs).
    /// </summary>
    public static WebApplication SeedData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StoreManagement.DatabaseSeeding");

        if (context.Stores.Any())
        {
            logger.LogInformation("Stores already exist. Skipping seeding.");
            return app;
        }

        // Must match Identity seeder seller IDs.
        var techStoreSellerId = "11111111-1111-1111-1111-111111111111";
        var homeStoreSellerId = "22222222-2222-2222-2222-222222222222";

        var techStore = Store.Create(techStoreSellerId, "Tech Store", "Electronics and gadgets from leading brands.");
        techStore.Verify();

        var homeStore = Store.Create(homeStoreSellerId, "Home Store", "Home essentials, kitchen tools, and lifestyle products.");
        homeStore.Verify();

        context.Stores.AddRange(techStore, homeStore);
        context.SaveChanges();

        logger.LogInformation("Seeded 2 stores: Tech Store ({TechStoreId}), Home Store ({HomeStoreId})",
            techStore.Id, homeStore.Id);

        return app;
    }
}
