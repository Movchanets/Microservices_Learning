using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Payment.Domain.Aggregates;
using Payment.Infrastructure.External;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PaymentDbContext>());
        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();

        return services;
    }
}
