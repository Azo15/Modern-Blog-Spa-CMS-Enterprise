using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using EntityLayer.Concrete;
using MediatR;

namespace BusinessLayer.CQRS.Handlers
{
    public class GetAllBlogsQueryHandler : IRequestHandler<GetAllBlogsQuery, List<Blog>>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheService _cacheService;

        public GetAllBlogsQueryHandler(IBlogService blogService, ICacheService cacheService)
        {
            _blogService = blogService;
            _cacheService = cacheService;
        }

        public async Task<List<Blog>> Handle(GetAllBlogsQuery request, CancellationToken cancellationToken)
        {
            string cacheKey = request.CategoryId.HasValue && request.CategoryId.Value > 0
                ? $"blogs_category_{request.CategoryId.Value}"
                : "all_blogs_cache";

            var cachedBlogs = await _cacheService.GetAsync<List<Blog>>(cacheKey);
            if (cachedBlogs != null && cachedBlogs.Any())
            {
                return cachedBlogs.Where(b => !b.IsDeleted && b.PublishStatus == EntityLayer.Enum.PublishStatus.Published).ToList();
            }

            List<Blog> blogs;
            if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            {
                blogs = await _blogService.GetListAsync(
                    filter: b => b.CatagoryId == request.CategoryId.Value && !b.IsDeleted && b.PublishStatus == EntityLayer.Enum.PublishStatus.Published,
                    orderBy: q => q.OrderByDescending(b => b.CreatedDate),
                    includes: new System.Linq.Expressions.Expression<Func<Blog, object>>[] { b => b.Catagory, b => b.AppUser }
                );
            }
            else
            {
                blogs = await _blogService.GetListAsync(
                    filter: b => !b.IsDeleted && b.PublishStatus == EntityLayer.Enum.PublishStatus.Published,
                    orderBy: q => q.OrderByDescending(b => b.CreatedDate),
                    includes: new System.Linq.Expressions.Expression<Func<Blog, object>>[] { b => b.Catagory, b => b.AppUser }
                );
            }

            if (blogs != null && blogs.Any())
            {
                await _cacheService.SetAsync(cacheKey, blogs, TimeSpan.FromMinutes(15));
            }

            return blogs ?? new List<Blog>();
        }
    }

    public class GetBlogByIdQueryHandler : IRequestHandler<GetBlogByIdQuery, Blog?>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheService _cacheService;

        public GetBlogByIdQueryHandler(IBlogService blogService, ICacheService cacheService)
        {
            _blogService = blogService;
            _cacheService = cacheService;
        }

        public async Task<Blog?> Handle(GetBlogByIdQuery request, CancellationToken cancellationToken)
        {
            string cacheKey = $"blog_detail_{request.Id}";
            var cachedBlog = await _cacheService.GetAsync<Blog>(cacheKey);
            if (cachedBlog != null)
            {
                return !cachedBlog.IsDeleted && cachedBlog.PublishStatus == EntityLayer.Enum.PublishStatus.Published ? cachedBlog : null;
            }

            var blog = await _blogService.FirstOrDefaultAsync(
                predicate: b => b.Id == request.Id && !b.IsDeleted && b.PublishStatus == EntityLayer.Enum.PublishStatus.Published,
                includes: new System.Linq.Expressions.Expression<Func<Blog, object>>[] { b => b.Catagory, b => b.AppUser }
            );

            if (blog != null)
            {
                await _cacheService.SetAsync(cacheKey, blog, TimeSpan.FromMinutes(30));
            }

            return blog;
        }
    }

    public class CreateBlogCommandHandler(IBlogWorkflowService workflow) : IRequestHandler<CreateBlogCommand, int>
    {
        public Task<int> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
            => workflow.CreateAsync(new Blog { Title = request.Title, SubTitle = request.SubTitle, Content = request.Content,
                Image = request.Image, CatagoryId = request.CatagoryId }, cancellationToken);
    }

    public class UpdateBlogCommandHandler(IBlogWorkflowService workflow) : IRequestHandler<UpdateBlogCommand, bool>
    {
        public Task<bool> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
            => workflow.UpdateAsync(request.Id, request.Title, request.SubTitle, request.Content, request.CatagoryId, request.Image, cancellationToken);
    }

    public class DeleteBlogCommandHandler(IBlogWorkflowService workflow) : IRequestHandler<DeleteBlogCommand, bool>
    {
        public Task<bool> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
            => workflow.DeleteAsync(request.Id, cancellationToken);
    }
}
