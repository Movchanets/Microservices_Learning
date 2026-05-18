// BuyerIdUserIdProvider unit tests.
// Validates that the provider prefers JWT claims over query string,
// and falls back to query string when no claims are present.

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Notification.Worker.Hubs;

namespace Notification.UnitTests.Consumers;

public class UserIdProviderTests
{
    [Fact]
    public void ImplementsIUserIdProvider()
    {
        var provider = new BuyerIdUserIdProvider();
        provider.Should().BeAssignableTo<IUserIdProvider>();
    }

    [Fact]
    public void ResolveBuyerId_WithClaim_ReturnsClaimValue()
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "buyer-from-jwt")]));

        var result = BuyerIdUserIdProvider.ResolveBuyerId(user, null);

        result.Should().Be("buyer-from-jwt");
    }

    [Fact]
    public void ResolveBuyerId_WithQueryStringOnly_ReturnsQueryString()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["buyerId"] = "buyer-from-query"
        });

        var result = BuyerIdUserIdProvider.ResolveBuyerId(null, query);

        result.Should().Be("buyer-from-query");
    }

    [Fact]
    public void ResolveBuyerId_WithBothClaimAndQuery_PrefersClaim()
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "buyer-from-jwt")]));

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["buyerId"] = "buyer-from-query"
        });

        var result = BuyerIdUserIdProvider.ResolveBuyerId(user, query);

        result.Should().Be("buyer-from-jwt");
    }

    [Fact]
    public void ResolveBuyerId_WithNoClaimNoQuery_ReturnsNull()
    {
        var result = BuyerIdUserIdProvider.ResolveBuyerId(null, null);

        result.Should().BeNullOrEmpty();
    }
}
