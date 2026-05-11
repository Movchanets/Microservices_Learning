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
                Identity.Domain.Enums.UserRole.Admin);
            context.Users.Add(admin);
        }

        if (!context.Users.Any(u => u.Email == Email.Create("buyer@test.com")))
        {
            logger.LogInformation("Seeding buyer user...");
            var buyer = Identity.Domain.Aggregates.User.Create(
                "buyer@test.com",
                hasher.Hash("P@ssw0rd"),
                "Test",
                "Buyer",
                Identity.Domain.Enums.UserRole.Buyer);
            context.Users.Add(buyer);
        }

        if (context.ChangeTracker.HasChanges())
        {
            context.SaveChanges();
            logger.LogInformation("Test users seeded.");
        }

        return app;
    }
}
