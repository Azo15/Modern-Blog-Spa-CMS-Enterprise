using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Dtos
{
    public class BlogDto
    {
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [Display(Name = "Slug")]
        public string Slug { get; set; }

        [Display(Name = "Alt Başlık")]
        public string SubTitle { get; set; }

        [Display(Name = "İçerik")]
        public string Content { get; set; }

        [Display(Name = "Görsel")]
        public string Image { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Yayınlandı")]
        public bool IsPublish { get; set; }

        [Display(Name = "Kategori")]
        public int CatagoryId { get; set; }

        [Display(Name = "Kategori Adı")]
        public string CatagoryTitle { get; set; }

        [Display(Name = "Yazar")]
        public string AppUserFullName { get; set; }
    }

    public class BlogCreateDto
    {
        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [Display(Name = "Alt Başlık")]
        public string SubTitle { get; set; }

        [Display(Name = "İçerik")]
        public string Content { get; set; }

        [Display(Name = "Görsel")]
        public string? Image { get; set; }

        [Display(Name = "Kategori Adı")]
        public int CatagoryId { get; set; }
    }

    public class BlogUpdateDto
    {
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [Display(Name = "Alt Başlık")]
        public string SubTitle { get; set; }

        [Display(Name = "İçerik")]
        public string Content { get; set; }

        [Display(Name = "Görsel")]
        public string? Image { get; set; }

        [Display(Name = "Kategori Adı")]
        public int CatagoryId { get; set; }

        [Display(Name = "Yayınlandı")]
        public bool IsPublish { get; set; }
    }
}
