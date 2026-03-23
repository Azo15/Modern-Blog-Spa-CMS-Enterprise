using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class Service : BaseEntity
    {
        [Display(Name = "Servis Adı")]
        public string? Title { get; set; }

        [Display(Name = "Servis Fiyatı")]
        public string? Content { get; set; }

        [Display(Name = "Servis Detayları")]
        public string? Details { get; set; }

        [Display(Name = "Servis Fotoğrafı")]
        public string? Image { get; set; }

        [Display(Name = "Öne Çıkma")]
        public bool Prominence { get; set; }
    }
}
