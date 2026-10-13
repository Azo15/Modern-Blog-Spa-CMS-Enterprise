using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccesLayer.Configurations;

public sealed class BlogFavoriteConfiguration : IEntityTypeConfiguration<BlogFavorite>
{
    public void Configure(EntityTypeBuilder<BlogFavorite> builder)
    {
        builder.HasIndex(x => new { x.AppUserId, x.BlogId }).IsUnique();
        builder.HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Blog).WithMany().HasForeignKey(x => x.BlogId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.BlogId);
    }
}
