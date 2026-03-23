using EntityLayer.Concrete;

namespace WebUI.Models
{
    public class FooterViewModel
    {
        public Contact Contact { get; set; } = new Contact();
        public List<Service> Services { get; set; } = new List<Service>();
        public string? Logo { get; set; }
        public string? SiteTitle { get; set; }
    }
}
