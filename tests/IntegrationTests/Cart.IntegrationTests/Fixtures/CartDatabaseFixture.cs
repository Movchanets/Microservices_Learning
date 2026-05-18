using Cart.Domain.Aggregates;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Repositories;
using Marketplace.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Cart.IntegrationTests;

public sealed class CartDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = PostgresContainerFactory.Create("cart_tests");
    private readonly RedisContainer _redisContainer = RedisContainerFactory.Create();

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        var services = new ServiceCollection();

        services.AddDbContext<CartDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = _redisContainer.GetConnectionString();
        });

        services.AddScoped<CartRepository>();
        services.AddScoped<ICartRepository>(sp => sp.GetRequiredService<CartRepository>());
        services.AddScoped<IProductPriceRepository, ProductPriceRepository>();

        ServiceProvider = services.BuildServiceProvider();

        // Create schema
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    public IServiceScope CreateScope() => ServiceProvider.CreateScope();
}

[CollectionDefinition("Cart collection")]
public class CartCollection : ICollectionFixture<CartDatabaseFixture>
{
}
