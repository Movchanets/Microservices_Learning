// BuyerIdUserIdProvider unit tests.
// Validates the SignalR IUserIdProvider implementation that maps the x-buyer-id
// HTTP header to SignalR's user concept for targeted message delivery.

using FluentAssertions;
using Notification.Worker.Hubs;

namespace Notification.UnitTests.Consumers;

public class UserIdProviderTests
{
    [Fact]
    public void BuyerIdUserIdProvider_ImplementsIUserIdProvider()
    {
        var provider = new BuyerIdUserIdProvider();

        provider.Should().BeAssignableTo<Microsoft.AspNetCore.SignalR.IUserIdProvider>();
    }
}
