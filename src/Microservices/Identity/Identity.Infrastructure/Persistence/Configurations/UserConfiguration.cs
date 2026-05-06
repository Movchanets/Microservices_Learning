using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        // Email value object → stored as string column
        builder.Property(u => u.Email)
            .HasConversion(
                e => e.Value,
                v => Email.Create(v))
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        // PasswordHash value object → stored as string column
        builder.Property(u => u.PasswordHash)
            .HasConversion(
                p => p.Hash,
                v => PasswordHash.FromHashedValue(v))
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // Owned type for RefreshToken
        builder.OwnsOne(u => u.CurrentRefreshToken, rt =>
        {
            rt.Property(r => r.Token).HasMaxLength(256);
            rt.Property(r => r.ExpiresAt);
            rt.Property(r => r.CreatedAt);
        });

        // Ignore domain events — dispatched in-memory, not persisted
        builder.Ignore(u => u.DomainEvents);
    }
}
