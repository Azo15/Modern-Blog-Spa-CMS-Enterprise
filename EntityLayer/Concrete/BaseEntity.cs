using System.ComponentModel.DataAnnotations;

namespace EntityLayer.Concrete
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        [Display(Name = "Düzenleme Tarihi")]
        public DateTime UpdatedDate { get; set; }

        [Display(Name = "Eklenme Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Yayın Durumu")]
        public bool IsPublish { get; set; }
    }
}