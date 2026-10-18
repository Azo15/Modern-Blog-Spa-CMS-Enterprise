using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class AppUser : IdentityUser<int>
    {
        [Display(Name = "Ad")]
        public string? FirstName { get; set; }

        [Display(Name = "Soyad")]
        public string? LastName { get; set; }

        [Display(Name = "Ad Soyad")]
        public string? FullName => $"{FirstName} {LastName}";

        [Display(Name = "Profil Fotoğrafı")]
        public string? Image { get; set; }

        [Display(Name = "Aktiflik Durumu")]
        public bool IsActive { get; set; } = true;

        public DateTime? PrivacyAcknowledgedAtUtc { get; set; }
        [MaxLength(30)] public string? PrivacyNoticeVersion { get; set; }

        public int FailedLoginCount { get; set; }

        public DateTime? LastFailedLogin { get; set; }

        // Navigation property
        [Display(Name = "Bloglar")]
        public ICollection<Blog>? Blogs { get; set; }
    }
}
