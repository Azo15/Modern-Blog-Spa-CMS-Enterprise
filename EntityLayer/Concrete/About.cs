using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class About : BaseEntity
    {
        [Display(Name = "Başlık")]
        public string? Title { get; set; }

        [Display(Name = "Açıklama")]
        public string? Content { get; set; }

        [Display(Name = "Kart Başlık 1")]
        public string? CardTitle1 { get; set; }

        [Display(Name = "Kart Başlık 2")]
        public string? CardTitle2 { get; set; }

        [Display(Name = "Kart İçeriği 1")]
        public string? CardContent1 { get; set; }

        [Display(Name = "Kart İçeriği 2")]
        public string? CardContent2 { get; set; }

        [Display(Name = "Hakkımızda Fotoğrafı")]
        public string? Image { get; set; }
    }
}
