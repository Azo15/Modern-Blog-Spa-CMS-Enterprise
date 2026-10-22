using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class ResetPasswordViewModel
{
    [Required, StringLength(20)] public string UserId { get; set; } = "";
    [Required, StringLength(4096)] public string Token { get; set; } = "";
    [Required(ErrorMessage = "Yeni parolanızı girin."), StringLength(128, MinimumLength = 6, ErrorMessage = "Parola 6–128 karakter olmalı."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Parolanızı tekrar girin."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = "";
}
