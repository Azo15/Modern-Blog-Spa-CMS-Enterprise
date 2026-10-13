using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccesLayer.Configurations;

public sealed class BlogReactionConfiguration : IEntityTypeConfiguration<BlogReaction>
{
    public void Configure(EntityTypeBuilder<BlogReaction> builder)
    {
        builder.HasIndex(x => new { x.AppUserId, x.BlogId }).IsUnique();
        builder.HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Blog).WithMany().HasForeignKey(x => x.BlogId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BlogId, x.ReactionType });
        builder.ToTable(t => t.HasCheckConstraint("CK_BlogReactions_ReactionType", "[ReactionType] IN (1, 2, 3)"));
    }
}
