using BuildingBlocks.SharedContracts.Events.StoreManagement;
using MassTransit;
using MediatR;
using StoreManagement.Domain.Events;

namespace StoreManagement.Infrastructure.Messaging;

/// <summary>
/// Handles the StoreVerifiedDomainEvent and publishes it as an integration event to the message bus.
/// </summary>
public sealed class StoreVerifiedEventHandler(
    IPublishEndpoint publishEndpoint)
    : INotificationHandler<StoreVerifiedDomainEvent>
{
    public async Task Handle(StoreVerifiedDomainEvent notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new StoreVerifiedIntegrationEvent(
            notification.StoreId,
            notification.SellerId,
            DateTime.UtcNow), cancellationToken);
    }
}
