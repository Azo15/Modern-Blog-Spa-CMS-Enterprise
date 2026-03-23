using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFSiteTableDal : GenericRepository<SiteTable>, ISiteTableDal
    {
        public EFSiteTableDal(AppDbContext context) : base(context)
        {
        }
    }
}
