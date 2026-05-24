using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Infrastructure.Database;
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
        => app.ApplyMigrations<OrderingDbContext>("Ordering");
}
