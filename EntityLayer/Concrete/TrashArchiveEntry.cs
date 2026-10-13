namespace EntityLayer.Concrete;

/// <summary>Archive manifest; the original soft-deleted row and its image remain available for restore.</summary>
public sealed class TrashArchiveEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public DateTime ArchivedAtUtc { get; set; } = DateTime.UtcNow;
    public string SnapshotJson { get; set; } = string.Empty;
}
