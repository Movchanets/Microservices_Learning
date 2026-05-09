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
}
