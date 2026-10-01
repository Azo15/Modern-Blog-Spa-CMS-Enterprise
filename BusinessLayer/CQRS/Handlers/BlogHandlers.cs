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
                return cachedBlogs;
            }

            List<Blog> blogs;
            if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            {
                blogs = await _blogService.GetListAsync(
                    filter: b => b.CatagoryId == request.CategoryId.Value && b.IsPublish == true,
                    orderBy: q => q.OrderByDescending(b => b.CreatedDate),
                    includes: new System.Linq.Expressions.Expression<Func<Blog, object>>[] { b => b.Catagory, b => b.AppUser }
                );
            }
            else
            {
                blogs = await _blogService.GetListAsync(
                    filter: b => b.IsPublish == true,
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
                return cachedBlog;
            }

            var blog = await _blogService.FirstOrDefaultAsync(
                predicate: b => b.Id == request.Id,
                includes: new System.Linq.Expressions.Expression<Func<Blog, object>>[] { b => b.Catagory, b => b.AppUser }
            );

            if (blog != null)
            {
                await _cacheService.SetAsync(cacheKey, blog, TimeSpan.FromMinutes(30));
            }

            return blog;
        }
    }

    public class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, int>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheService _cacheService;

        public CreateBlogCommandHandler(IBlogService blogService, ICacheService cacheService)
        {
            _blogService = blogService;
            _cacheService = cacheService;
        }

        public async Task<int> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = new Blog
            {
                Title = request.Title,
                SubTitle = request.SubTitle,
                Content = request.Content,
                Image = request.Image ?? string.Empty,
                CatagoryId = request.CatagoryId,
                AppUserId = request.AppUserId,
                IsPublish = true
            };

            await _blogService.AddAsync(blog);

            // 📌 Önbellek Temizleme (Cache Invalidation)
            await _cacheService.RemoveAsync("all_blogs_cache");
            await _cacheService.RemoveAsync($"blogs_category_{request.CatagoryId}");

            return blog.Id;
        }
    }

    public class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, bool>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheService _cacheService;

        public UpdateBlogCommandHandler(IBlogService blogService, ICacheService cacheService)
        {
            _blogService = blogService;
            _cacheService = cacheService;
        }

        public async Task<bool> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await _blogService.GetByIDAsync(request.Id);
            if (blog == null) return false;

            blog.Title = request.Title;
            blog.SubTitle = request.SubTitle;
            blog.Content = request.Content;
            if (!string.IsNullOrEmpty(request.Image))
            {
                blog.Image = request.Image;
            }
            blog.CatagoryId = request.CatagoryId;

            var result = await _blogService.UpdateAsync(blog);

            if (result)
            {
                // 📌 Önbellek Temizleme (Cache Invalidation)
                await _cacheService.RemoveAsync("all_blogs_cache");
                await _cacheService.RemoveAsync($"blog_detail_{request.Id}");
                await _cacheService.RemoveAsync($"blogs_category_{request.CatagoryId}");
            }

            return result;
        }
    }

    public class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, bool>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheService _cacheService;

        public DeleteBlogCommandHandler(IBlogService blogService, ICacheService cacheService)
        {
            _blogService = blogService;
            _cacheService = cacheService;
        }

        public async Task<bool> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
        {
            var blog = await _blogService.GetByIDAsync(request.Id);
            if (blog != null)
            {
                await _cacheService.RemoveAsync("all_blogs_cache");
                await _cacheService.RemoveAsync($"blog_detail_{request.Id}");
                await _cacheService.RemoveAsync($"blogs_category_{blog.CatagoryId}");
            }

            return await _blogService.DeleteAsync(request.Id);
        }
    }
}
