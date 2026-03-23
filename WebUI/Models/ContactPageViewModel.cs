using EntityLayer.Concrete;

namespace WebUI.Models
{
    public class ContactPageViewModel
    {
        public ContactForm? ContactForm { get; set; } = new ContactForm();
        public Contact? Contact { get; set; } = new Contact();
    }
}
