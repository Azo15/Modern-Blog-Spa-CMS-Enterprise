using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresinizi girin."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin."), StringLength(254)]
    public string Email { get; set; } = "";
}
