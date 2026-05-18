using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Identity.Domain.ValueObjects;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Provides startup migration helpers for the Identity service.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Identity DbContext.
    /// Useful for local development and integration tests.
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Identity.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            logger.LogInformation("Applying Identity database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Identity database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying Identity database migrations.");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Seeds initial test data for the Identity microservice.
    /// </summary>
    public static WebApplication SeedData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Identity.DatabaseSeeding");
        var hasher = scope.ServiceProvider.GetService<Identity.Application.Interfaces.IPasswordHasher>();
        var adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var buyerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var storeSellerOneId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var storeSellerTwoId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        if (hasher == null)
        {
            logger.LogWarning("IPasswordHasher not registered. Skipping seeding.");
            return app;
        }

        if (!context.Users.Any(u => u.Email == Email.Create("admin@marketplace.com")))
        {
            logger.LogInformation("Seeding admin user...");
            var admin = Identity.Domain.Aggregates.User.Create(
                "admin@marketplace.com",
                hasher.Hash("P@ssw0rd123!"),
                "Admin",
                "User",
                Identity.Domain.Enums.UserRole.Admin,
                userId: adminId);
            context.Users.Add(admin);
        }

        if (!context.Users.Any(u => u.Email == Email.Create("buyer@marketplace.com")))
        {
            logger.LogInformation("Seeding buyer user...");
            var buyer = Identity.Domain.Aggregates.User.Create(
                "buyer@marketplace.com",
                hasher.Hash("P@ssw0rd123!"),
                "Test",
                "Buyer",
                Identity.Domain.Enums.UserRole.Buyer,
                userId: buyerId);
            context.Users.Add(buyer);
        }

        if (!context.Users.Any(u => u.Email == Email.Create("store.tech@marketplace.com")))
        {
            logger.LogInformation("Seeding seller user for Tech Store...");
            var techStoreSeller = Identity.Domain.Aggregates.User.Create(
                "store.tech@marketplace.com",
                hasher.Hash("P@ssw0rd123!"),
                "Tech",
                "Store",
                Identity.Domain.Enums.UserRole.Seller,
                userId: storeSellerOneId);
            context.Users.Add(techStoreSeller);
        }

        if (!context.Users.Any(u => u.Email == Email.Create("store.home@marketplace.com")))
        {
            logger.LogInformation("Seeding seller user for Home Store...");
            var homeStoreSeller = Identity.Domain.Aggregates.User.Create(
                "store.home@marketplace.com",
                hasher.Hash("P@ssw0rd123!"),
                "Home",
                "Store",
                Identity.Domain.Enums.UserRole.Seller,
                userId: storeSellerTwoId);
            context.Users.Add(homeStoreSeller);
        }

        if (context.ChangeTracker.HasChanges())
        {
            context.SaveChanges();
            logger.LogInformation("Seed users created: admin + 2 seller stores.");
        }

        return app;
    }
}
