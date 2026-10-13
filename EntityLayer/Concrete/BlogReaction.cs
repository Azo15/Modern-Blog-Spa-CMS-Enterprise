using EntityLayer.Enum;

namespace EntityLayer.Concrete;

public sealed class BlogReaction
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public ReactionType ReactionType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
