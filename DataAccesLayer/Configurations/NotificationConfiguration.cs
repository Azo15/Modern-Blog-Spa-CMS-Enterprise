using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DataAccesLayer.Configurations;
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Content).HasMaxLength(1000).IsRequired();
        b.Property(x => x.EventKey).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.RecipientUserId, x.EventKey }).IsUnique();
        b.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAtUtc });
        b.HasIndex(x => new { x.Type, x.RelatedRecordId });
        b.HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
    }
}
