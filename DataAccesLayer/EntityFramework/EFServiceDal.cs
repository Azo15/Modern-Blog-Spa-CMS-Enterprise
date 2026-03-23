using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFServiceDal: GenericRepository<Service>, IServiceDal
    {
        public EFServiceDal(AppDbContext context) : base(context)
        {
        }
    }
}
