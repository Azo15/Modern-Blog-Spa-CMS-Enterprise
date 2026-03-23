using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class AboutCardManager : GenericManager<AboutCard>, IAboutCardService
    {
        public AboutCardManager(IGenericDal<AboutCard> AboutCardDal) : base(AboutCardDal)
        {
        }
    }
}
