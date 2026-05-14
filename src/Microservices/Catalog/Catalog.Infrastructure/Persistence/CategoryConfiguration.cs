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

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.ParentCategoryId);
    }
}
