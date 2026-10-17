using EntityLayer.Enum;
using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public class ContactForm : BaseEntity
    {
        public bool IsRead { get; set; }
        [Display(Name = "Adınız Soyadınız")]
        public string? FullName { get; set; }

        [Display(Name = "E-posta Adresiniz")]
        public string? Email { get; set; }

        [Display(Name = "Telefon Numaranız")]
        public string? Phone { get; set; }

        [Display(Name = "Mesaj Konusu")]
        public string? Subject { get; set; }

        [Display(Name = "Mesajınız")]
        public string? Message { get; set; }

        [Display(Name = "Durumu")]
        public ReservationStatus Status { get; set; } = ReservationStatus.SoforBeklemede;
    }
}
