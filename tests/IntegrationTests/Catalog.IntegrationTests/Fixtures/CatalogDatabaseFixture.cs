using Catalog.Infrastructure.Persistence;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        // Mock IPublishEndpoint to avoid actual MassTransit connection but allow Outbox functionality
        var mockPublishEndpoint = new Mock<IPublishEndpoint>();
        services.AddScoped(_ => mockPublishEndpoint.Object);

        // Add MediatR and the handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Catalog.Infrastructure.EventPublishing.ProductCreatedDomainEventHandler).Assembly));

        // Repositories
        services.AddScoped<Catalog.Domain.Aggregates.IProductRepository, Catalog.Infrastructure.Repositories.ProductRepository>();
        services.AddScoped<Catalog.Domain.Entities.ICategoryRepository, Catalog.Infrastructure.Repositories.CategoryRepository>();

        // Logging
        services.AddLogging();

        // Setup MassTransit Outbox to write directly to EF
        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
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