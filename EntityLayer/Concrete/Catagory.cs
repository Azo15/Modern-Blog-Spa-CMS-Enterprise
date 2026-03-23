using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class Catagory : BaseEntity
    {
        [Display(Name = "Blog Kategori Adı")]
        public string? Title { get; set; }

        public ICollection<Blog>? Blogs { get; set; }
    }
}
