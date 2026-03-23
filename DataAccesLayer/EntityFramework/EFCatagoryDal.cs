using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFCatagoryDal : GenericRepository<Catagory>, ICatagoryDal
    {
        public EFCatagoryDal(AppDbContext context) : base(context)
        {
        }
    }
}
