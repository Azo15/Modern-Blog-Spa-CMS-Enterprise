using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccesLayer.Configurations;

public sealed class TrashArchiveEntryConfiguration : IEntityTypeConfiguration<TrashArchiveEntry>
{
    public void Configure(EntityTypeBuilder<TrashArchiveEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();
        builder.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();
    }
}
