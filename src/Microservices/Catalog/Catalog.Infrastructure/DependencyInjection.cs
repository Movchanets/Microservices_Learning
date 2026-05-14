using BuildingBlocks.SharedContracts.Abstractions;
using Catalog.Application.Interfaces;
using Catalog.Domain.Aggregates;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Repositories;
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
        // EF Core with PostgreSQL (non-pooled, required because DbContext uses scoped IPublisher)
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("catalog-db"),
                npgsql => npgsql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());

        return services;
    }
}
