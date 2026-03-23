using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class Galary : BaseEntity
    {
        [Display(Name = "Galeri Medya Dosyası")]
        public string? Image { get; set; }

        [Display(Name = "Başlık")]
        public string? Title { get; set; }
    }
}
