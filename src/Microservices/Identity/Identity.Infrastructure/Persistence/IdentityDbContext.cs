using BuildingBlocks.SharedContracts.Abstractions;
using Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Represents the Entity Framework Core database context for the Identity microservice.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IUnitOfWork
{
    /// <summary>
    /// Gets the collection of Users in the database.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Configures the schema needed for the identity context.
    /// Rationale: Automatically applies all entity configurations implemented via <see cref="IEntityTypeConfiguration{TEntity}"/>
    /// within this assembly, ensuring clean separation of mapping logic from the context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
