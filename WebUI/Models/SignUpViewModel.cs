using System.ComponentModel.DataAnnotations;

namespace WebUI.Models
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage = "Lütfen E Postasınızı yazınız")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-mail adresi giriniz.")]
        [Display(Name = "E Posta")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Şifrenizi giriniz.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [Display(Name = "Sifreniz")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarını giriniz.")]
        [Compare("Password", ErrorMessage = "Şifreler aynı değil.")]
        [Display(Name = "Sifre Tekrarı")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Ad")]
        [Required(ErrorMessage = "Lütfen adını yazınız")]
        public string? FirstName { get; set; }

        [Display(Name = "Soyad")]
        [Required(ErrorMessage = "Lütfen Soyadınızı yazınız")]
        public string? LastName { get; set; }

        [Display(Name = "Doğum Tarihi")]
        public DateTime? BirthDate { get; set; }
    }
}
