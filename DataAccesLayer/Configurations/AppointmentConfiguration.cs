using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccesLayer.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.Property(x => x.ServiceTitleSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.AppointmentDate).HasColumnType("date");
        builder.Property(x => x.TimeSlot).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.GiftCode).HasMaxLength(40);
        builder.Property(x => x.UpdatedDate).IsConcurrencyToken();
        builder.Property(x => x.ReferenceCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasDefaultValue(AppointmentStatus.Pending).IsConcurrencyToken();
        builder.HasIndex(x => x.ReferenceCode).IsUnique();
        builder.HasIndex(x => x.ClientRequestId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.AppointmentDate, x.TimeSlot });
        builder.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class AppointmentEmailDeliveryConfiguration : IEntityTypeConfiguration<AppointmentEmailDelivery>
{
    public void Configure(EntityTypeBuilder<AppointmentEmailDelivery> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.AppointmentId, x.IsConfirmation }).IsUnique();
        builder.HasIndex(x => new { x.SentAtUtc, x.LastEnqueuedAtUtc });
        builder.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.MessageId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LeaseToken).HasMaxLength(40);
        builder.Property(x => x.HangfireJobId).HasMaxLength(100);
        builder.Property(x => x.LastError).HasMaxLength(2000);
    }
}
