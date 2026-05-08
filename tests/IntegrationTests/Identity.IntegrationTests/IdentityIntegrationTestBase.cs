using Identity.Domain.Aggregates;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.IntegrationTests;

/// <summary>
/// Base class for Identity integration tests.
/// Provides access to a real PostgreSQL-backed DbContext and repository.
/// </summary>
public abstract class IdentityIntegrationTestBase(IdentityPostgresFixture fixture)
{
    protected IdentityPostgresFixture Fixture { get; } = fixture;

    protected IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    protected IUserRepository CreateRepository(IdentityDbContext context) => new UserRepository(context);
}
