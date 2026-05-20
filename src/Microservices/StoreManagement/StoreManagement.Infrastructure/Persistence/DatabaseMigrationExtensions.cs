using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Infrastructure.Database;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;

namespace StoreManagement.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
        => app.ApplyMigrations<StoreDbContext>("StoreManagement");


    /// <summary>
    /// Seeds initial stores for the dev environment.
    /// Must run after Identity seeding (depends on seller user IDs).
    /// </summary>
    public static async Task SeedDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StoreManagement.DatabaseSeeding");

        if (context.Stores.Any())
        {
            logger.LogInformation("Stores already exist. Skipping seeding.");
            return;
        }

        // Well-known IDs — must match Identity seeder seller IDs and Catalog seeder store IDs.
        var techStoreSellerId = "11111111-1111-1111-1111-111111111111";
        var homeStoreSellerId = "22222222-2222-2222-2222-222222222222";
        var techStoreId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var homeStoreId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var techStore = Store.Create(techStoreSellerId, "Tech Store", "Electronics and gadgets from leading brands.", techStoreId);
        techStore.Verify();

        var homeStore = Store.Create(homeStoreSellerId, "Home Store", "Home essentials, kitchen tools, and lifestyle products.", homeStoreId);
        homeStore.Verify();

        // Clear domain events — seeding should not publish integration events.
        techStore.ClearDomainEvents();
        homeStore.ClearDomainEvents();

        context.Stores.AddRange(techStore, homeStore);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded 2 stores: Tech Store ({TechStoreId}), Home Store ({HomeStoreId})",
            techStore.Id, homeStore.Id);

        return;
    }
}
