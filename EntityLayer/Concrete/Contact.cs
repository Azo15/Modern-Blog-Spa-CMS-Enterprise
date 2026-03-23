using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class Contact : BaseEntity
    {
        [Display(Name = "E-posta")]
        public string? Mail { get; set; }

        [Display(Name = "Adres")]
        public string? Adres { get; set; }

        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Display(Name = "Instagram URL")]
        public string? InstagramUrl { get; set; }

        [Display(Name = "Facebook URL")]
        public string? FacebookUrl { get; set; }

        [Display(Name = "Twitter URL")]
        public string? TwitterUrl { get; set; }

        [Display(Name = "Youtube URL")]
        public string? YoutubeUrl { get; set; }
    }
}
