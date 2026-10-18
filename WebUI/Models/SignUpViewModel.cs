using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class SignUpViewModel
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, Phone, StringLength(30), RegularExpression(@"^\+?[0-9 ()-]{10,30}$")]
    public string Phone { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor."), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
    [Validation.MustBeTrue(ErrorMessage = "KVKK aydınlatma metnini okuduğunuzu belirtin.")]
    public bool KvkkAccepted { get; set; }
}
