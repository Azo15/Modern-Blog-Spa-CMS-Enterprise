using System.ComponentModel.DataAnnotations;

namespace WebUI.Models
{
    public class SignUpViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required]
        [Compare("Password")]
        [Display(Name = "ConfirmPassword")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "FirstName")]
        [Required]
        public string? FirstName { get; set; }

        [Display(Name = "LastName")]
        [Required]
        public string? LastName { get; set; }

        [Display(Name = "Doğum Tarihi")]
        public DateTime? BirthDate { get; set; }
    }
}
