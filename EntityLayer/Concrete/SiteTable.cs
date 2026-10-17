using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class SiteTable : BaseEntity
    {
        [Display(Name = "Google Serach Console")]
        public string? GoogleSearch { get; set; }

        [Display(Name = "Site Başlığı")]
        public string? SiteTitle { get; set; }

        [Display(Name = "Site Urlsi")]
        public string? SiteUrl { get; set; }

        [Display(Name = "Sosyal medya fotoğrafı")]
        public string? SocialImage { get; set; }

        [Display(Name = "Site Logosu")]
        public string? SiteLogo { get; set; }

        [Display(Name = "SMTP Host")]
        public string? Host { get; set; }

        [Display(Name = "SMTP Port")]
        public string? Port { get; set; }

        [Display(Name = "E-Posta")]
        public string? Mail { get; set; }

        [Display(Name = "SMTP Şifresi")]
        public string? Password { get; set; }
        public string? SmtpUsername { get; set; }
        public bool EnableSsl { get; set; } = true;
    }
}
