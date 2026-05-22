using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Infrastructure.Database;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;

namespace StoreManagement.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
        => app.ApplyMigrations<StoreDbContext>("StoreManagement");
}
