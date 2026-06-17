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
    /// Executes an action with retry logic for transient database failures.
    /// Used by ApplyMigrations to handle cases where PostgreSQL isn't ready yet
    /// (e.g., during Aspire startup where the DB container may still be initializing).
    /// </summary>
    public static void ApplyWithRetry(
        Action action,
        string serviceName,
        ILogger logger,
        int maxRetries = 5,
        TimeSpan? delay = null)
    {
        var retryDelay = delay ?? TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                action();
                return; // success
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex,
                    "{Service} migration attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}s...",
                    serviceName, attempt, maxRetries, retryDelay.TotalSeconds);
                Thread.Sleep(retryDelay);
            }
        }
    }

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

        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        logger.LogInformation("Applying {Service} database migrations...", serviceName);

        ApplyWithRetry(
            () => dbContext.Database.Migrate(),
            serviceName,
            logger);

        logger.LogInformation("{Service} database migrations applied successfully.", serviceName);

        return app;
    }
}
