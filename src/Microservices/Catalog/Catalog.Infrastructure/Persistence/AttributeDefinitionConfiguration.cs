using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class AttributeDefinitionConfiguration : IEntityTypeConfiguration<AttributeDefinition>
{
    public void Configure(EntityTypeBuilder<AttributeDefinition> builder)
    {
        builder.ToTable("AttributeDefinitions");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Key).IsRequired().HasMaxLength(64);
        builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(128);
        builder.Property(a => a.Target).IsRequired();
        builder.Property(a => a.ValueType).IsRequired();

        // List<string> works natively with Npgsql jsonb — no ValueConverter needed.
        // (Dictionary<string, string> in SkuConfiguration does require one.)
        builder.Property(a => a.AllowedValues)
            .HasColumnType("jsonb");

        // Relationship: AttributeDefinition belongs to Category
        builder.HasOne<Category>()
            .WithMany(c => c.AttributeDefinitions)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => new { a.CategoryId, a.Key }).IsUnique();
        builder.HasIndex(a => a.CategoryId);
    }
}
