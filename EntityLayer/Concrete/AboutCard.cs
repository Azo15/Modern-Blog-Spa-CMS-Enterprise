using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class AboutCard : BaseEntity
    {

        [Display(Name = "Başlık")]
        public string? Title { get; set; }

        [Display(Name = "İçerik")]
        public string? Content { get; set; }
    }
}
