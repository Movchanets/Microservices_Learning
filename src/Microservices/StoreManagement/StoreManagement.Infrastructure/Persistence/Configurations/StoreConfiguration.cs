using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreManagement.Domain.Aggregates;
using StoreManagement.Domain.Enumerations;

namespace StoreManagement.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SellerId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.RejectionReason).HasMaxLength(1000);

        builder.Property(s => s.VerificationStatus)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<VerificationStatus>(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.SellerId).IsUnique();
        builder.HasIndex(s => s.VerificationStatus);
    }
}
