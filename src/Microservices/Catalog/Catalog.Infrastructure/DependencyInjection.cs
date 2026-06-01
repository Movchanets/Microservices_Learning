using BuildingBlocks.Infrastructure.Database.Interceptors;
using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Repositories;
using Catalog.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain event dispatcher interceptor (singleton-safe, takes IServiceScopeFactory)
        services.AddSingleton<DomainEventDispatcherInterceptor>();

        // EF Core with PostgreSQL
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("catalog-db"),
                npgsql => npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());

        // Ordering API client for inter-service communication
        services.AddHttpClient<IOrderingApiClient, OrderingApiClient>(client =>
            client.BaseAddress = new Uri("https://ordering-api"));

        return services;
    }
}
