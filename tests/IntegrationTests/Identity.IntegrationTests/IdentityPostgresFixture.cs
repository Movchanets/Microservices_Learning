using Identity.Infrastructure.Persistence;
using Marketplace.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Identity.IntegrationTests;

/// <summary>
/// Shared PostgreSQL fixture for Identity integration tests.
/// Starts a real PostgreSQL container and applies the current migrations once.
/// </summary>
public sealed class IdentityPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = PostgresContainerFactory.Create("identity_integration_tests");

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(ConnectionString));

        using var serviceProvider = services.BuildServiceProvider();
        await serviceProvider.ApplyMigrationsAsync<IdentityDbContext>();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
