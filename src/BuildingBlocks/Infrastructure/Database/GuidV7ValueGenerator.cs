using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace BuildingBlocks.Infrastructure.Database;

/// <summary>
/// Generates Guid v7 (time-ordered) values for entity keys.
/// Guid v7 is sortable by creation time, making it ideal for primary keys
/// and improving index performance in PostgreSQL.
/// </summary>
public sealed class GuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}
