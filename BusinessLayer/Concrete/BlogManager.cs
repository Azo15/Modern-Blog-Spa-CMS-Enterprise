using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class BlogManager : GenericManager<Blog>, IBlogService
    {
        public BlogManager(IGenericDal<Blog> BlogDal) : base(BlogDal)
        {
        }
    }
}
