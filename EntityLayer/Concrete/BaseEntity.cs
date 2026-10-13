using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        [Display(Name = "Eklenme Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Oluşturan Kullanıcı")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Düzenleme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Güncelleyen Kullanıcı")]
        public string? UpdatedBy { get; set; }

        [Display(Name = "Yayın Durumu")]
        public bool IsPublish { get; set; } = true;

        [Display(Name = "Silindi Mi (Soft Delete)")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Silinme Tarihi")]
        public DateTime? DeletedDate { get; set; }

        [Display(Name = "Silen Kullanıcı")]
        public string? DeletedBy { get; set; }
    }
}