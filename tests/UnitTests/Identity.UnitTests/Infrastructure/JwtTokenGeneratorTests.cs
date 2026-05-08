using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Identity.UnitTests.Infrastructure;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateAccessToken_WhenSecretIsMissing_ShouldThrowInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var generator = new JwtTokenGenerator(configuration);
        var user = User.Create("buyer@example.com", "hashed-password", "Jane", "Doe");

        var act = () => generator.GenerateAccessToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret not configured*");
    }

    [Fact]
    public void GenerateAccessToken_WithConfiguredSecret_ShouldIncludeExpectedClaims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-tests-min-32-characters",
                ["Jwt:Issuer"] = "identity-tests",
                ["Jwt:Audience"] = "gateway-tests"
            })
            .Build();

        var generator = new JwtTokenGenerator(configuration);
        var user = User.Create("seller@example.com", "hashed-password", "Jane", "Doe", UserRole.Seller);

        var token = generator.GenerateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("identity-tests");
        jwt.Audiences.Should().Contain("gateway-tests");
        jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id.ToString());
        jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("seller@example.com");
        jwt.Claims.First(x => x.Type == ClaimTypes.Role).Value.Should().Be("Seller");
        jwt.Claims.First(x => x.Type == "firstName").Value.Should().Be("Jane");
        jwt.Claims.First(x => x.Type == "lastName").Value.Should().Be("Doe");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueBase64Tokens()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-tests-min-32-characters"
            })
            .Build();

        var generator = new JwtTokenGenerator(configuration);

        var token1 = generator.GenerateRefreshToken();
        var token2 = generator.GenerateRefreshToken();

        token1.Should().NotBe(token2);
        var bytes = Convert.FromBase64String(token1);
        bytes.Length.Should().Be(64);
    }
}
