using System.ComponentModel.DataAnnotations;

namespace WebUI.Models
{
    public class UserListViewModel
    {
        public string? Id { get; set; }

        [Display(Name = "Ad Soyad")]
        public string? FullName { get; set; }

        [Display(Name = "E-posta Adresi")]
        public string? Email { get; set; }

        [Display(Name = "Fotoğraf")]
        public string? Image { get; set; }
    }
}
