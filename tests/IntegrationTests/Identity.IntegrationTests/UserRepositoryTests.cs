using FluentAssertions;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;

namespace Identity.IntegrationTests;

[Collection(IdentityIntegrationCollection.Name)]
public sealed class UserRepositoryTests(IdentityDatabaseFixture fixture)
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
    public async Task Add_DuplicateEmail_ThrowsDbUpdateException()
    {
        await using var context1 = CreateDbContext();
        var repository1 = CreateRepository(context1);

        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var user1 = User.Create(email, "password", "First", "User", UserRole.Buyer);

        repository1.Add(user1);
        await context1.SaveChangesAsync();

        await using var context2 = CreateDbContext();
        var repository2 = CreateRepository(context2);

        var user2 = User.Create(email, "password", "Second", "User", UserRole.Buyer);
        repository2.Add(user2);

        var action = async () => await context2.SaveChangesAsync();
        await action.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }

    [Fact]
    public async Task GetByEmail_WhenUserExists_ReturnsUser()
    {
        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var email = $"repo-user-{Guid.NewGuid():N}@example.com";
        var user = User.Create(email, "hashed-password", "Repo", "Tester", UserRole.Admin);

        repository.Add(user);
        await context.SaveChangesAsync();

        var loadedUser = await repository.GetByEmailAsync(email);

        loadedUser.Should().NotBeNull();
        loadedUser!.Id.Should().Be(user.Id);
        loadedUser.Email.Value.Should().Be(email);
        loadedUser.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserMissing_ReturnsNull()
    {
        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var missingEmail = $"missing-user-{Guid.NewGuid():N}@example.com";

        var loadedUser = await repository.GetByEmailAsync(missingEmail);

        loadedUser.Should().BeNull();
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

    [Fact]
    public async Task Exists_WhenUserExists_ReturnsTrue()
    {
        var email = $"exists-user-{Guid.NewGuid():N}@example.com";
        var user = User.Create(email, "hashed-password", "Repo", "Tester", UserRole.Seller);

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);
        repository.Add(user);
        await context.SaveChangesAsync();

        var exists = await repository.ExistsAsync(email);

        exists.Should().BeTrue();
    }
}
