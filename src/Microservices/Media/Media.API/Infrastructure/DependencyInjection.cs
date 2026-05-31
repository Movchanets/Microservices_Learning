using BuildingBlocks.Infrastructure.Database.Interceptors;
using BuildingBlocks.SharedContracts.Abstractions;
using Media.API.Application.Interfaces;
using Media.API.Domain;
using Media.API.Infrastructure.Persistence;
using Media.API.Infrastructure.Repositories;
using Media.API.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Media.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMediaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Domain event dispatcher interceptor (singleton-safe)
        services.AddSingleton<DomainEventDispatcherInterceptor>();

        // EF Core with PostgreSQL
        services.AddDbContext<MediaDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("media-db"),
                npgsql => npgsql.MigrationsAssembly(typeof(MediaDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>());
        });

        // Repositories
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IGalleryRepository, GalleryRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MediaDbContext>());

        // Storage
        services.AddScoped<IMediaStorageService, AzureBlobStorageService>();

        return services;
    }
}
