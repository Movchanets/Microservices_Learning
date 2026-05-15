using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Domain.Aggregates;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Repositories;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());

        return services;
    }
}
