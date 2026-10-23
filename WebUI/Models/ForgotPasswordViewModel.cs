using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class ForgotPasswordViewModel
{
    public const string GoogleAccountHelp = "Google ile kaydolduysanız Google ile devam edebilirsiniz. Ayrı bir parolanız yoksa e-postadaki bağlantıyla ilk parolanızı da belirleyebilirsiniz.";
    private string email = "";
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta adresinizi girin."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin."), StringLength(254)]
    public string Email { get => email; set => email = value?.Trim() ?? ""; }
}
