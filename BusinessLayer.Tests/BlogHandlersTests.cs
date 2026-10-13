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
                new Blog { Id = 1, Title = "Cached Blog 1", IsPublish = true, PublishStatus = EntityLayer.Enum.PublishStatus.Published },
                new Blog { Id = 2, Title = "Cached Blog 2", IsPublish = true, PublishStatus = EntityLayer.Enum.PublishStatus.Published }
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
                new Blog { Id = 10, Title = "DB Blog 1", IsPublish = true, PublishStatus = EntityLayer.Enum.PublishStatus.Published }
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

    }
}
