using FluentAssertions;
using Identity.Infrastructure.Services;

namespace Identity.UnitTests.Infrastructure;

public sealed class PasswordHasherServiceTests
{
    [Fact]
    public void HashAndVerify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hasher = new PasswordHasherService();

        var hash = hasher.Hash("P@ssw0rd!");
        var isValid = hasher.Verify("P@ssw0rd!", hash);

        hash.Should().NotBeNullOrWhiteSpace();
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.Hash("P@ssw0rd!");

        var isValid = hasher.Verify("wrong-password", hash);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ShouldProduceDifferentHashes()
    {
        var hasher = new PasswordHasherService();

        var firstHash = hasher.Hash("P@ssw0rd!");
        var secondHash = hasher.Hash("P@ssw0rd!");

        firstHash.Should().NotBe(secondHash);
    }
}
