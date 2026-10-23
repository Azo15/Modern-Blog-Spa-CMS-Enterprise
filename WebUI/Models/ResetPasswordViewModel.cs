using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class ResetPasswordViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Sıfırlama bağlantısı geçersiz."), StringLength(20)] public string UserId { get; set; } = "";
    [Required(ErrorMessage = "Sıfırlama bağlantısı eksik."), StringLength(4096), RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Sıfırlama bağlantısı geçersiz.")] public string Token { get; set; } = "";
    [Display(Name = "Yeni parola")]
    [Required(ErrorMessage = "Yeni parolanızı girin."), StringLength(128, MinimumLength = 6, ErrorMessage = "Parola 6–128 karakter olmalı."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Display(Name = "Parola tekrarı")]
    [Required(ErrorMessage = "Parolanızı tekrar girin."), StringLength(128), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!int.TryParse(UserId, out var id) || id <= 0)
            yield return new ValidationResult("Sıfırlama bağlantısı geçersiz.", [nameof(UserId)]);
    }
}
