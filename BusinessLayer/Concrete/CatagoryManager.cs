using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class CatagoryManager : GenericManager<Catagory>, ICatagoryService
    {
        public CatagoryManager(IGenericDal<Catagory> CatagoryDal) : base(CatagoryDal)
        {
        }
    }
}
