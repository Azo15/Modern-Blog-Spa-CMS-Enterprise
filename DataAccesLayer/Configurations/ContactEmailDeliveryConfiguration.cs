using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccesLayer.Configurations;

public sealed class ContactEmailDeliveryConfiguration : IEntityTypeConfiguration<ContactEmailDelivery>
{
    public void Configure(EntityTypeBuilder<ContactEmailDelivery> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ContactFormId).IsUnique();
        builder.HasIndex(x => new { x.SentAtUtc, x.LastEnqueuedAtUtc });
        builder.HasOne(x => x.ContactForm).WithOne().HasForeignKey<ContactEmailDelivery>(x => x.ContactFormId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.MessageId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LeaseToken).HasMaxLength(40);
        builder.Property(x => x.HangfireJobId).HasMaxLength(100);
        builder.Property(x => x.LastError).HasMaxLength(2000);
    }
}
