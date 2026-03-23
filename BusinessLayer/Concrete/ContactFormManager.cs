using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class ContactFormManager : GenericManager<ContactForm>, IContactFormService
    {
        public ContactFormManager(IGenericDal<ContactForm> ContactDal) : base(ContactDal)
        {
        }
    }
}
