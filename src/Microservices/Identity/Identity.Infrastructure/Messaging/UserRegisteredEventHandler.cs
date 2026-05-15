using BuildingBlocks.SharedContracts.Events.Identity;
using Identity.Domain.Events;
using MassTransit;
using MediatR;

namespace Identity.Infrastructure.Messaging;

/// <summary>
/// Handles the UserRegisteredDomainEvent and publishes it as an integration event to the message bus.
/// </summary>
public sealed class UserRegisteredEventHandler(
    IPublishEndpoint publishEndpoint)
    : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new UserRegisteredIntegrationEvent(
            notification.UserId,
            notification.Email,
            notification.FirstName,
            notification.LastName,
            notification.Role,
            DateTime.UtcNow), cancellationToken);
    }
}
