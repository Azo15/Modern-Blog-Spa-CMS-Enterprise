using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class SignUpViewModel
{
    [Required, StringLength(100)] [Display(Name = "Ad")] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] [Display(Name = "Soyad")] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] [Display(Name = "E-posta")] public string Email { get; set; } = string.Empty;
    [Required, Phone, StringLength(30), RegularExpression(@"^\+?[0-9 ()-]{10,30}$")]
    [Display(Name = "Telefon")] public string Phone { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] [Display(Name = "Parola")] public string Password { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8), Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor."), DataType(DataType.Password)] [Display(Name = "Parola tekrarı")] public string ConfirmPassword { get; set; } = string.Empty;
    [Validation.MustBeTrue(ErrorMessage = "KVKK aydınlatma metnini okuduğunuzu belirtin.")]
    [Display(Name = "KVKK aydınlatma metni")] public bool KvkkAccepted { get; set; }
}
