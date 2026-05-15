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
