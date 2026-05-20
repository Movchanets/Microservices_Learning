using BuildingBlocks.Infrastructure.Database.Interceptors;
using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Infrastructure.Persistence;
using StoreManagement.Infrastructure.Repositories;

namespace StoreManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStoreManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain event dispatcher interceptor
        services.AddSingleton<DomainEventDispatcherInterceptor>();

        services.AddDbContext<StoreDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("store-db"),
                npgsql => npgsql.MigrationsAssembly(typeof(StoreDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<StoreDbContext>());

        return services;
    }
}
