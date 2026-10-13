using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer.Concrete
{
    public class Blog : BaseEntity
    {
        public EntityLayer.Enum.PublishStatus PublishStatus { get; set; } = EntityLayer.Enum.PublishStatus.Draft;
        [Display(Name = "Başlık")]
        public string? Title { get; set; }

        [Display(Name = "Alt Başlık")]
        public string? SubTitle { get; set; }

        [Display(Name = "İçerik")]
        public string? Content { get; set; }

        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [Display(Name = "Fotoğraf")]
        public string? Image { get; set; }


        //----------------------------------

        [ForeignKey("AppUser")]
        [Display(Name = "Kullanıcı ID")]
        public int? AppUserId { get; set; }
        [Display(Name = "Kullanıcı")]
        public AppUser? AppUser { get; set; }

        [ForeignKey("Catagory")]
        [Display(Name = "Kategori ID")]
        public int? CatagoryId { get; set; }
        [Display(Name = "Kategori")]
        public Catagory? Catagory { get; set; }
    }
}
