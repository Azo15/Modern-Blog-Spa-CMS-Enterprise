using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class GalaryManager : GenericManager<Galary>, IGalaryService
    {
        public GalaryManager(IGenericDal<Galary> GalaryDal) : base(GalaryDal)
        {
        }
    }
}
