using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Infrastructure.Database;
using Inventory.Domain.Aggregates;

namespace Inventory.Infrastructure.Data;

/// <summary>
/// Provides startup migration helpers for the Inventory service.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Inventory DbContext.
    /// Useful for local development and integration tests.
    /// </summary>
    public static WebApplication ApplyMigrations(this WebApplication app)
        => app.ApplyMigrations<InventoryDbContext>("Inventory");
}
