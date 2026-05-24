using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Marketplace.IntegrationTests.Shared;

/// <summary>
/// Factory helpers for PostgreSQL Testcontainers used by integration tests.
/// </summary>
public static class PostgresContainerFactory
{
    /// <summary>
    /// Creates a PostgreSQL container with a predictable configuration for tests.
    /// </summary>
    public static PostgreSqlContainer Create(
        string databaseName,
        string username = "postgres",
        string password = "postgres") => new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(databaseName)
            .WithUsername(username)
            .WithPassword(password)
            .Build();
}

/// <summary>
/// Shared helper for applying EF Core migrations in integration tests.
/// </summary>
public static class RedisContainerFactory
{
    public static RedisContainer Create()
    {
        return new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }
}

public static class ServiceProviderMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations for the specified DbContext type.
    /// </summary>
    public static async Task ApplyMigrationsAsync<TDbContext>(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
