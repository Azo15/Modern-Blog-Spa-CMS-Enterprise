using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFAboutCardDal: GenericRepository<AboutCard>, IAboutCardDal
    {
        public EFAboutCardDal(AppDbContext context) : base(context)
        {
        }
    }
}
