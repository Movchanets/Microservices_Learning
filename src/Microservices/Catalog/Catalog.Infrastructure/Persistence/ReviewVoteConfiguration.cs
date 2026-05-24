using Catalog.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

public sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("ReviewVotes");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ReviewId).IsRequired();
        builder.Property(v => v.UserId).IsRequired();
        builder.Property(v => v.IsHelpful).IsRequired();

        builder.HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();
    }
}
