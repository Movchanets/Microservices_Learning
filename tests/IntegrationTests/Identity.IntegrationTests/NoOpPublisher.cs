using MediatR;

namespace Identity.IntegrationTests;

/// <summary>
/// No-op implementation of IPublisher for integration tests that don't need to verify domain event publishing.
/// </summary>
public sealed class NoOpPublisher : IPublisher
{
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        => Task.CompletedTask;

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
