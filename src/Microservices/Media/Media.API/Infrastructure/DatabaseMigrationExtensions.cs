using BuildingBlocks.Infrastructure.Database;
using Media.API.Infrastructure.Persistence;

namespace Media.API.Infrastructure;

public static class DatabaseMigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
        => app.ApplyMigrations<MediaDbContext>("Media");
}
