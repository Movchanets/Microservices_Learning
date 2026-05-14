using FluentAssertions;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.IntegrationTests;

[Collection(IdentityIntegrationCollection.Name)]
public sealed class IdentityDatabaseTests(IdentityDatabaseFixture fixture)
    : IdentityIntegrationTestBase(fixture)
{
    [Fact]
    public async Task ApplyMigrations_ShouldLeaveNoPendingMigrations()
    {
        await using var context = CreateDbContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        pendingMigrations.Should().BeEmpty();
    }
}
