using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.StoreManagement;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes StoreVerifiedIntegrationEvent and updates the seller's role from Buyer to Seller.
/// </summary>
public sealed class StoreVerifiedConsumer(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<StoreVerifiedConsumer> logger)
    : IConsumer<StoreVerifiedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<StoreVerifiedIntegrationEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Store verified for seller {SellerId}, updating role to Seller", evt.SellerId);

        if (!Guid.TryParse(evt.SellerId, out var sellerGuid))
        {
            logger.LogWarning("Invalid SellerId format: {SellerId}", evt.SellerId);
            return;
        }

        var user = await userRepository.GetByIdAsync(sellerGuid, context.CancellationToken);
        if (user is null)
        {
            logger.LogWarning("User {SellerId} not found for role update", evt.SellerId);
            return;
        }

        user.AddRole(UserRole.Seller);
        user.SetStoreId(evt.StoreId);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("User {SellerId} role updated to Seller", evt.SellerId);
    }
}
