using Catalog.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ProductId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.UserName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(300);
        builder.Property(r => r.Text).IsRequired().HasMaxLength(5000);
        builder.Property(r => r.IsVerifiedPurchase).IsRequired();
        builder.Property(r => r.PhotoUrls).HasColumnType("jsonb");
        builder.Property(r => r.HelpfulCount).IsRequired();
        builder.Property(r => r.NotHelpfulCount).IsRequired();
        builder.Property(r => r.SellerResponse).HasMaxLength(5000);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
    }
}
