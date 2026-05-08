using FluentAssertions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.IntegrationTests;

[Collection(IdentityIntegrationCollection.Name)]
public sealed class UserRepositoryTests(IdentityPostgresFixture fixture)
    : IdentityIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Add_ThenGetById_ReturnsPersistedUser()
    {
        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var email = $"alice-{Guid.NewGuid():N}@example.com";
        var user = User.Create(email, "hashed-password", "Alice", "Anderson", UserRole.Buyer);

        repository.Add(user);
        await context.SaveChangesAsync();

        var persisted = await repository.GetByIdAsync(user.Id);

        persisted.Should().NotBeNull();
        persisted!.Email.Value.Should().Be(email);
        persisted.FirstName.Should().Be("Alice");
        persisted.LastName.Should().Be("Anderson");
        persisted.Role.Should().Be(UserRole.Buyer);
    }

    [Fact]
    public async Task GetByEmail_WhenUserExists_ReturnsUser()
    {
        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var email = $"bob-{Guid.NewGuid():N}@example.com";
        var user = User.Create(email, "hashed-password", "Bob", "Builder", UserRole.Admin);

        repository.Add(user);
        await context.SaveChangesAsync();

        var persisted = await repository.GetByEmailAsync(email);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(user.Id);
        persisted.Email.Value.Should().Be(email);
        persisted.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Exists_WhenUserMissing_ReturnsFalse()
    {
        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var missingEmail = $"missing-{Guid.NewGuid():N}@example.com";

        var exists = await repository.ExistsAsync(missingEmail);

        exists.Should().BeFalse();
    }
}
