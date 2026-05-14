using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
}
