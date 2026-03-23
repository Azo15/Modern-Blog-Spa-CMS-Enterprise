using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFGalaryDal : GenericRepository<Galary>, IGalaryDal
    {
        public EFGalaryDal(AppDbContext context) : base(context)
        {
        }
    }
}
