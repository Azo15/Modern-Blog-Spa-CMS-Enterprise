using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFSliderDal: GenericRepository<Slider>, ISliderDal
    {
        public EFSliderDal(AppDbContext context) : base(context)
        {
        }
    }
}
