using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Database;

/// <summary>
/// Generic EF Core migration helpers shared across all microservices.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the specified DbContext type.
    /// </summary>
    public static WebApplication ApplyMigrations<TDbContext>(this WebApplication app, string serviceName)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger($"{serviceName}.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            logger.LogInformation("Applying {Service} database migrations...", serviceName);
            dbContext.Database.Migrate();
            logger.LogInformation("{Service} database migrations applied successfully.", serviceName);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying {Service} database migrations.", serviceName);
            throw;
        }

        return app;
    }
}
