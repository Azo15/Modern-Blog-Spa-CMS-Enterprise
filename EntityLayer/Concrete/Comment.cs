using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete;

public class Comment : BaseEntity
{
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Children { get; set; } = new List<Comment>();
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}
