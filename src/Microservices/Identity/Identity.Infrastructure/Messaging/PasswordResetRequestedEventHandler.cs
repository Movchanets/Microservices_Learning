using BuildingBlocks.SharedContracts.Events.Identity;
using Identity.Domain.Events;
using MassTransit;
using MediatR;

namespace Identity.Infrastructure.Messaging;

/// <summary>
/// Handles the PasswordResetRequested domain event and publishes it as an integration event to the message bus.
/// </summary>
public sealed class PasswordResetRequestedEventHandler(
    IPublishEndpoint publishEndpoint)
    : INotificationHandler<PasswordResetRequestedEvent>
{
    public async Task Handle(PasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new PasswordResetRequestedIntegrationEvent(
            notification.UserId,
            notification.Email,
            notification.Token,
            DateTime.UtcNow), cancellationToken);
    }
}
