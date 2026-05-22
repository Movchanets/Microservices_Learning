using BuildingBlocks.SharedContracts.Events.StoreManagement;
using MassTransit;
using MediatR;
using StoreManagement.Domain.Events;

namespace StoreManagement.Infrastructure.Messaging;

/// <summary>
/// Handles the StoreCreatedDomainEvent and publishes it as an integration event to the message bus.
/// This allows Identity to set the StoreId on the seller's user record immediately.
/// </summary>
public sealed class StoreCreatedEventHandler(
    IPublishEndpoint publishEndpoint)
    : INotificationHandler<StoreCreatedDomainEvent>
{
    public async Task Handle(StoreCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new StoreCreatedIntegrationEvent(
            notification.StoreId,
            notification.SellerId,
            DateTime.UtcNow), cancellationToken);
    }
}
