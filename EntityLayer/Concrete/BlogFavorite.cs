namespace EntityLayer.Concrete;

// Favorites are reversible interactions, not soft-deleted content records.
public sealed class BlogFavorite
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
