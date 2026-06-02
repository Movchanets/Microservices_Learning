using BuildingBlocks.SharedContracts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Payment.Domain.Aggregates;
using Payment.Infrastructure.External;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;

namespace Payment.Infrastructure;

/// <summary>
/// Registers Payment infrastructure services: repositories, payment gateway,
/// and DbContext. Called from Payment.API Program.cs.
/// </summary>
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
