using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class SiteTableManager : GenericManager<SiteTable>, ISiteTableService
    {
        public SiteTableManager(IGenericDal<SiteTable> MessageDal) : base(MessageDal)
        {
        }
    }
}
