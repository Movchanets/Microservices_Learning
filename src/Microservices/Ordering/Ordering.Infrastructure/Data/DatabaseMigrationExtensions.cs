using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure.Data;

/// <summary>
/// Provides startup migration helpers for the Ordering service.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Ordering DbContext.
    /// Useful for local development and integration tests.
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Ordering.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            logger.LogInformation("Applying Ordering database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Ordering database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying Ordering database migrations.");
            throw;
        }

        return app;
    }
}
