using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StoreManagement.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("StoreManagement.DatabaseMigration");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            logger.LogInformation("Applying StoreManagement database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("StoreManagement database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred while applying StoreManagement database migrations.");
            throw;
        }

        return app;
    }
}
