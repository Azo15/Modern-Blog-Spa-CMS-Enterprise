using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Handlers;
using BusinessLayer.CQRS.Queries;
using EntityLayer.Concrete;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BusinessLayer.Tests
{
    public class BlogHandlersTests
    {
        private readonly Mock<IBlogService> _mockBlogService;
        private readonly Mock<ICacheService> _mockCacheService;

        public BlogHandlersTests()
        {
            _mockBlogService = new Mock<IBlogService>();
            _mockCacheService = new Mock<ICacheService>();
        }

        [Fact]
        public async Task GetAllBlogsQueryHandler_ShouldReturnBlogsFromCache_WhenCacheExists()
        {
            // Arrange
            var cachedBlogs = new List<Blog>
            {
                new Blog { Id = 1, Title = "Cached Blog 1", IsPublish = true },
                new Blog { Id = 2, Title = "Cached Blog 2", IsPublish = true }
            };

            _mockCacheService
                .Setup(c => c.GetAsync<List<Blog>>("all_blogs_cache"))
                .ReturnsAsync(cachedBlogs);

            var handler = new GetAllBlogsQueryHandler(_mockBlogService.Object, _mockCacheService.Object);

            // Act
            var result = await handler.Handle(new GetAllBlogsQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Title.Should().Be("Cached Blog 1");

            _mockBlogService.Verify(b => b.GetListAsync(
                It.IsAny<Expression<Func<Blog, bool>>>(),
                It.IsAny<Func<IQueryable<Blog>, IOrderedQueryable<Blog>>>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Blog, object>>[]>()
            ), Times.Never);
        }

        [Fact]
        public async Task GetAllBlogsQueryHandler_ShouldFetchFromDbAndSetCache_WhenCacheIsEmpty()
        {
            // Arrange
            _mockCacheService
                .Setup(c => c.GetAsync<List<Blog>>("all_blogs_cache"))
                .ReturnsAsync((List<Blog>?)null);

            var dbBlogs = new List<Blog>
            {
                new Blog { Id = 10, Title = "DB Blog 1", IsPublish = true }
            };

            _mockBlogService
                .Setup(b => b.GetListAsync(
                    It.IsAny<Expression<Func<Blog, bool>>>(),
                    It.IsAny<Func<IQueryable<Blog>, IOrderedQueryable<Blog>>>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool>(),
                    It.IsAny<Expression<Func<Blog, object>>[]>()
                ))
                .ReturnsAsync(dbBlogs);

            var handler = new GetAllBlogsQueryHandler(_mockBlogService.Object, _mockCacheService.Object);

            // Act
            var result = await handler.Handle(new GetAllBlogsQuery(), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Title.Should().Be("DB Blog 1");

            _mockCacheService.Verify(c => c.SetAsync(
                "all_blogs_cache",
                It.IsAny<List<Blog>>(),
                It.IsAny<TimeSpan?>()
            ), Times.Once);
        }

        [Fact]
        public async Task CreateBlogCommandHandler_ShouldAddBlog_AndInvalidateCache()
        {
            // Arrange
            var command = new CreateBlogCommand(
                Title: "Yeni Test Blog",
                SubTitle: "Test Alt Başlık",
                Content: "Test İçerik",
                Image: "test.jpg",
                CatagoryId: 1,
                AppUserId: 1
            );

            _mockBlogService
                .Setup(b => b.AddAsync(It.IsAny<Blog>()))
                .ReturnsAsync(true);

            var handler = new CreateBlogCommandHandler(_mockBlogService.Object, _mockCacheService.Object);

            // Act
            var blogId = await handler.Handle(command, CancellationToken.None);

            // Assert
            _mockBlogService.Verify(b => b.AddAsync(It.Is<Blog>(x => x.Title == "Yeni Test Blog")), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("all_blogs_cache"), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("blogs_category_1"), Times.Once);
        }

        [Fact]
        public async Task DeleteBlogCommandHandler_ShouldDeleteBlog_AndInvalidateCache()
        {
            // Arrange
            int blogId = 5;
            var existingBlog = new Blog { Id = blogId, CatagoryId = 2 };

            _mockBlogService.Setup(b => b.GetByIDAsync(blogId)).ReturnsAsync(existingBlog);
            _mockBlogService.Setup(b => b.DeleteAsync(blogId)).ReturnsAsync(true);

            var handler = new DeleteBlogCommandHandler(_mockBlogService.Object, _mockCacheService.Object);

            // Act
            var result = await handler.Handle(new DeleteBlogCommand(blogId), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _mockBlogService.Verify(b => b.DeleteAsync(blogId), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("all_blogs_cache"), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"blog_detail_{blogId}"), Times.Once);
        }
    }
}
