using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            var assembly = typeof(Identity.Infrastructure.DependencyInjection).Assembly;
            var dbContextType = assembly.GetType("Identity.Infrastructure.Persistence.IdentityDbContext")
                ?? throw new InvalidOperationException("Could not locate IdentityDbContext type.");

            var dbContext = scope.ServiceProvider.GetRequiredService(dbContextType);
            logger.LogInformation("Applying Identity database migrations...");

            var databaseProperty = dbContextType.GetProperty("Database")
                ?? throw new InvalidOperationException("Could not locate Database property on IdentityDbContext.");
            var database = databaseProperty.GetValue(dbContext)
                ?? throw new InvalidOperationException("Could not resolve IdentityDbContext.Database.");

            var migrateMethod = database.GetType().GetMethod("Migrate", Type.EmptyTypes)
                ?? throw new InvalidOperationException("Could not locate Migrate method on IdentityDbContext.Database.");

            migrateMethod.Invoke(database, null);
            logger.LogInformation("Identity database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying Identity database migrations.");
            throw;
        }

        return app;
    }
}
