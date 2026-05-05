using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Application.Interfaces;
using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core with PostgreSQL is added by Aspire AddNpgsqlDbContext

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Services
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
