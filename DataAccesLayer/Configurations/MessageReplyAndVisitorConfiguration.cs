using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccesLayer.Configurations;

public sealed class ContactReplyConfiguration : IEntityTypeConfiguration<ContactReply>
{
    public void Configure(EntityTypeBuilder<ContactReply> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.ContactForm).WithMany().HasForeignKey(x => x.ContactFormId).OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(x => !x.ContactForm.IsDeleted);
        b.Property(x => x.ReplyMessage).HasMaxLength(5000).IsRequired();
        b.Property(x => x.RepliedByUserId).HasMaxLength(256).IsRequired();
        b.Property(x => x.RepliedByUserName).HasMaxLength(256).IsRequired();
        b.Property(x => x.RecipientEmail).HasMaxLength(254).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(250).IsRequired();
        b.Property(x => x.MessageId).HasMaxLength(160).IsRequired();
        b.Property(x => x.LeaseToken).HasMaxLength(64);
        b.Property(x => x.HangfireJobId).HasMaxLength(100);
        b.Property(x => x.LastError).HasMaxLength(128);
        b.HasIndex(x => x.ClientRequestId).IsUnique();
        b.HasIndex(x => new { x.ContactFormId, x.RepliedAt });
        b.HasIndex(x => new { x.IsMailSent, x.LastEnqueuedAtUtc });
    }
}

public sealed class DailyVisitorStatConfiguration : IEntityTypeConfiguration<DailyVisitorStat>
{
    public void Configure(EntityTypeBuilder<DailyVisitorStat> b)
    {
        b.HasKey(x => x.Date);
        b.Property(x => x.TopVisitedPagesJson).HasMaxLength(4000);
    }
}

public sealed class VisitorLogConfiguration : IEntityTypeConfiguration<VisitorLog>
{
    public void Configure(EntityTypeBuilder<VisitorLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.VisitorHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Path).HasMaxLength(256).IsRequired();
        b.HasIndex(x => new { x.Date, x.VisitorHash });
        b.HasIndex(x => new { x.Date, x.Path });
        b.HasIndex(x => x.CreatedDate);
    }
}
