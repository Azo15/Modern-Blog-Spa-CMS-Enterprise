using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class SmtpSettingsForm : IValidatableObject
{
    [Required, StringLength(253), RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9.\-]*$", ErrorMessage = "Geçerli bir sunucu adı girin.")]
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    [Required, StringLength(254)] public string Username { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Mail { get; set; } = string.Empty;
    [StringLength(1024), DataType(DataType.Password)] public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public bool HasPassword { get; set; }
    public bool PasswordNeedsReplacement { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Port is not (465 or 587)) yield return new("Yalnızca 465 veya 587 kullanılabilir.", [nameof(Port)]);
        if (!EnableSsl) yield return new("Güvenli TLS bağlantısı zorunludur.", [nameof(EnableSsl)]);
    }
}
