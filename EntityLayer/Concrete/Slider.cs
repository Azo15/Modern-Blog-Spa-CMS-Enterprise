using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class Slider : BaseEntity
    {
        [Display(Name = "Slider Başlığı")]
        public string? Title { get; set; }

        [Display(Name = "Slider Açıklaması")]
        public string? Content { get; set; }

        [Display(Name = "Slider")]
        public string? Image { get; set; }
    }
}
