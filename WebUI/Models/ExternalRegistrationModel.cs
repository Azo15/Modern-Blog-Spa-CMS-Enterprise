using System.ComponentModel.DataAnnotations;
namespace WebUI.Models;
public sealed class ExternalRegistrationModel
{
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [Validation.MustBeTrue(ErrorMessage="KVKK aydınlatma metnini okuduğunuzu belirtin.")] public bool KvkkAccepted { get; set; }
    public string? ReturnUrl { get; set; }
}
