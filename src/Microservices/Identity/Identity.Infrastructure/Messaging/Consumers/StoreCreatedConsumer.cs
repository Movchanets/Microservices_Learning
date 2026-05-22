using BuildingBlocks.SharedContracts.Abstractions;
using BuildingBlocks.SharedContracts.Events.StoreManagement;
using Identity.Domain.Aggregates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes StoreCreatedIntegrationEvent and sets the StoreId on the seller's user record.
/// This allows the seller to create products immediately after creating a store,
/// without waiting for admin verification.
/// The Seller role is still only assigned after verification (via StoreVerifiedConsumer).
/// </summary>
public sealed class StoreCreatedConsumer(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<StoreCreatedConsumer> logger)
    : IConsumer<StoreCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<StoreCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Store created for seller {SellerId}, assigning StoreId {StoreId}", evt.SellerId, evt.StoreId);

        if (!Guid.TryParse(evt.SellerId, out var sellerGuid))
        {
            logger.LogWarning("Invalid SellerId format: {SellerId}", evt.SellerId);
            return;
        }

        var user = await userRepository.GetByIdAsync(sellerGuid, context.CancellationToken);
        if (user is null)
        {
            logger.LogWarning("User {SellerId} not found for StoreId assignment", evt.SellerId);
            return;
        }

        user.SetStoreId(evt.StoreId);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("StoreId {StoreId} assigned to user {SellerId}", evt.StoreId, evt.SellerId);
    }
}
