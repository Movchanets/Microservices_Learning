using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(128);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(128);

        // Relationship: AttributeDefinition belongs to Category
        builder.HasMany(c => c.AttributeDefinitions)
            .WithOne()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure EF Core uses the backing field for change tracking
        builder.Navigation(c => c.AttributeDefinitions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.ParentCategoryId);
    }
}
