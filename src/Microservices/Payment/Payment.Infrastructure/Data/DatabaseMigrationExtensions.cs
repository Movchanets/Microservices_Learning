using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Data;

/// <summary>
/// Provides startup migration helpers for the Payment service.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Payment DbContext.
    /// Useful for local development and integration tests.
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Payment.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            logger.LogInformation("Applying Payment database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Payment database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying Payment database migrations.");
            throw;
        }

        return app;
    }
}
