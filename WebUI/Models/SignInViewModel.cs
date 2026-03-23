using System.ComponentModel.DataAnnotations;

namespace WebUI.Models
{
    public class SignInViewModel
    {
        [Required(ErrorMessage = "Lütfen e-postanızı giriniz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-Posta")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Şifrenizi giriniz.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string? Password { get; set; }

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }
    }
}
