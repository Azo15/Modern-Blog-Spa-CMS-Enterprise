using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Dtos
{
    public class CatagoryDto
    {
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Display(Name = "Kategori Adı")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Yayınlandı")]
        public bool IsPublish { get; set; }
    }

    public class CatagoryCreateDto
    {
        [Display(Name = "Kategori Adı")]
        public string Title { get; set; } = string.Empty;
    }

    public class CatagoryUpdateDto
    {
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Display(Name = "Kategori Adı")]
        public string Title { get; set; } = string.Empty;
        
        [Display(Name = "Yayınlandı")]
        public bool IsPublish { get; set; }
    }
}
