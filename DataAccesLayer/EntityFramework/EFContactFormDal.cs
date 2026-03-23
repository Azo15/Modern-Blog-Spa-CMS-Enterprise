using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.Repository;
using EntityLayer.Concrete;

namespace DataAccesLayer.EntityFramework
{
    public class EFContactFormDal : GenericRepository<ContactForm>, IContactFormDal
    {
        public EFContactFormDal(AppDbContext context) : base(context)
        {
        }
    }
}
