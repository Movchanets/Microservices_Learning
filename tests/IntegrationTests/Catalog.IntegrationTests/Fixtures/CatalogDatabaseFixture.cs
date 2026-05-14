using Catalog.Infrastructure.Persistence;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Catalog.IntegrationTests.Fixtures;

public class CatalogDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("catalog_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var services = new ServiceCollection();

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(
                _dbContainer.GetConnectionString(),
                npgsql => npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)));

        // Add MediatR and the handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Catalog.Infrastructure.EventPublishing.ProductCreatedDomainEventHandler).Assembly));

        // Repositories
        services.AddScoped<Catalog.Domain.Aggregates.IProductRepository, Catalog.Infrastructure.Repositories.ProductRepository>();
        services.AddScoped<Catalog.Domain.Entities.ICategoryRepository, Catalog.Infrastructure.Repositories.CategoryRepository>();

        // Logging
        services.AddLogging();

        // Setup MassTransit with an InMemory transport and EF Core Outbox so that
        // IPublishEndpoint is MassTransit's outbox-backed implementation (not a mock).
        // Messages published by domain event handlers are captured in OutboxMessage
        // within the same EF transaction; no real broker is required.
        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.ConfigureEndpoints(ctx);
            });
        });

        ServiceProvider = services.BuildServiceProvider();

        // Apply migrations
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public IServiceScope CreateScope()
    {
        return ServiceProvider.CreateScope();
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<CatalogDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
