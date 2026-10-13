using System.Security.Claims;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BusinessLayer.Tests;

public sealed class BlogWorkflowTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ServiceProvider provider = AuthorizationTests.Provider();
    private readonly TestCurrentUser user = new();
    private readonly AppDbContext db;
    private readonly BlogWorkflowService workflow;
    private readonly Mock<ICacheService> cache = new();

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public ClaimsPrincipal User { get; set; } = AuthorizationTests.Principal(AppRoles.Author);
        public string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? Email => User.FindFirstValue(ClaimTypes.Email);
    }

    public BlogWorkflowTests()
    {
        connection.Open();
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options, user);
        db.Database.EnsureCreated();
        db.Users.AddRange(new AppUser { Id = 1, UserName = "author1" }, new AppUser { Id = 2, UserName = "author2" });
        db.Catagories.AddRange(new Catagory { Id = 1, Title = "A" }, new Catagory { Id = 2, Title = "B" });
        db.SaveChanges();
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        cache.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        workflow = new BlogWorkflowService(db, user, provider.GetRequiredService<IAuthorizationService>(), new HtmlContentSanitizer(), cache.Object);
    }

    private Task<int> Create() => workflow.CreateAsync(new Blog { Title = "Test title", Content = "<p>Hello</p>",
        CatagoryId = 1, Image = "uploads/blogs/test.jpg", AppUserId = 2, PublishStatus = PublishStatus.Published });

    [Fact]
    public async Task CreationIgnoresForgedOwnerAndPublishedStateAndSetsAudit()
    {
        var id = await Create();
        var blog = await db.Blogs.SingleAsync(b => b.Id == id);
        Assert.Equal(1, blog.AppUserId);
        Assert.Equal(PublishStatus.Draft, blog.PublishStatus);
        Assert.False(blog.IsPublish);
        Assert.Equal("1", blog.CreatedBy);
        Assert.Equal(DateTimeKind.Utc, blog.CreatedDate.Kind);
    }

    [Theory]
    [InlineData(AppRoles.RegisteredUser)]
    [InlineData(AppRoles.Moderator)]
    public async Task NonAuthorsCannotCreateContent(string role)
    {
        user.User = AuthorizationTests.Principal(role);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Create());
        Assert.Empty(await db.Blogs.ToListAsync());
    }

    [Fact]
    public async Task AnotherAuthorCannotUpdateDeleteOrSubmit()
    {
        var id = await Create();
        user.User = AuthorizationTests.Principal(AppRoles.Author, "2");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => workflow.UpdateAsync(id, "Forged", "", "", 1, null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => workflow.DeleteAsync(id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => workflow.ChangeStatusAsync(id, PublishStatus.PendingReview));
        Assert.Equal("Test title", (await db.Blogs.SingleAsync()).Title);
    }

    [Theory]
    [InlineData(PublishStatus.Published)]
    [InlineData(PublishStatus.Rejected)]
    public async Task AuthorCannotApproveOrReject(PublishStatus status)
    {
        var id = await Create();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => workflow.ChangeStatusAsync(id, status));
        Assert.Equal(PublishStatus.Draft, (await db.Blogs.SingleAsync()).PublishStatus);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ContentManager)]
    public async Task ManagerCanPublishAndEditWithoutChangingOriginalAuthor(string role)
    {
        var id = await Create();
        user.User = AuthorizationTests.Principal(role, "2");
        Assert.True(await workflow.ChangeStatusAsync(id, PublishStatus.Published));
        Assert.True(await workflow.UpdateAsync(id, "New title", "Subtitle", "<p>Safe</p><script>alert(1)</script>", 2, null));
        var blog = await db.Blogs.SingleAsync();
        Assert.Equal(1, blog.AppUserId);
        Assert.Equal(PublishStatus.Published, blog.PublishStatus);
        Assert.Equal("1", blog.CreatedBy);
        Assert.Equal("2", blog.UpdatedBy);
        Assert.DoesNotContain("<script", blog.Content);
        cache.Verify(c => c.RemoveByPrefixAsync("blogs_cat_1"), Times.AtLeastOnce);
        cache.Verify(c => c.RemoveByPrefixAsync("blogs_cat_2"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AuthorEditingPublishedContentMustGetApprovalAgain()
    {
        var id = await Create();
        user.User = AuthorizationTests.Principal(AppRoles.ContentManager, "2");
        await workflow.ChangeStatusAsync(id, PublishStatus.Published);
        user.User = AuthorizationTests.Principal(AppRoles.Author);
        await workflow.UpdateAsync(id, "Edited title", "", "Edited", 1, null);
        Assert.Equal(PublishStatus.Draft, (await db.Blogs.SingleAsync()).PublishStatus);
        Assert.False((await db.Blogs.SingleAsync()).IsPublish);
        await workflow.ChangeStatusAsync(id, PublishStatus.PendingReview);
        Assert.Equal(PublishStatus.PendingReview, (await db.Blogs.SingleAsync()).PublishStatus);
    }

    [Fact]
    public async Task SoftDeleteAndRestorePreserveImageAndAudit()
    {
        var image = Path.GetTempFileName();
        try
        {
            var id = await workflow.CreateAsync(new Blog { Title = "Delete test", Image = image });
            await workflow.DeleteAsync(id);
            Assert.Empty(await db.Blogs.ToListAsync());
            var deleted = await db.Blogs.IgnoreQueryFilters().SingleAsync();
            Assert.True(deleted.IsDeleted);
            Assert.Equal("1", deleted.DeletedBy);
            Assert.NotNull(deleted.DeletedDate);
            Assert.True(File.Exists(image));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => workflow.RestoreAsync(id));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => workflow.GetTrashAsync());
            user.User = AuthorizationTests.Principal(AppRoles.ContentManager, "2");
            Assert.Single(await workflow.GetTrashAsync());
            Assert.True(await workflow.RestoreAsync(id));
            var restored = await db.Blogs.SingleAsync();
            Assert.Equal(image, restored.Image);
            Assert.False(restored.IsDeleted);
            Assert.Null(restored.DeletedBy);
            Assert.Null(restored.DeletedDate);
            Assert.Equal("2", restored.UpdatedBy);
            Assert.Equal(PublishStatus.Draft, restored.PublishStatus);
            Assert.True(File.Exists(image));
        }
        finally { File.Delete(image); }
    }

    [Fact]
    public async Task DirectSoftDeleteAndBooleanSaveOverloadCannotBypassAudit()
    {
        var id = await Create();
        var blog = await db.Blogs.SingleAsync(b => b.Id == id);
        blog.IsDeleted = true;
        await db.SaveChangesAsync(true);
        Assert.Equal("1", blog.DeletedBy);
        Assert.NotNull(blog.DeletedDate);
    }

    [Fact]
    public async Task CreationSanitizesScriptEventHandlersAndJavascriptUrls()
    {
        var id = await workflow.CreateAsync(new Blog { Content = "<p>Safe</p><script>alert(1)</script><img src='x' onerror='alert(1)'><a href='javascript:alert(1)'>link</a>" });
        var content = (await db.Blogs.SingleAsync(b => b.Id == id)).Content!;
        Assert.Contains("<p>Safe</p>", content);
        Assert.DoesNotContain("<script", content);
        Assert.DoesNotContain("onerror", content);
        Assert.DoesNotContain("javascript:", content);
    }

    [Fact]
    public async Task PublicQueriesExcludeDraftPendingRejectedAndDeletedPosts()
    {
        var draft = await Create();
        var pending = await Create();
        var published = await Create();
        var rejected = await Create();
        var deleted = await Create();
        await workflow.ChangeStatusAsync(pending, PublishStatus.PendingReview);
        user.User = AuthorizationTests.Principal(AppRoles.ContentManager, "2");
        await workflow.ChangeStatusAsync(published, PublishStatus.Published);
        await workflow.ChangeStatusAsync(rejected, PublishStatus.Rejected);
        await workflow.ChangeStatusAsync(deleted, PublishStatus.Published);
        await workflow.DeleteAsync(deleted);
        foreach (var blog in await db.Blogs.IgnoreQueryFilters().ToListAsync()) blog.Slug = "post-" + blog.Id;
        await db.SaveChangesAsync();
        var list = new BusinessLayer.CQRS.Handlers.GetPublicBlogsQueryHandler(db, cache.Object);
        var detail = new BusinessLayer.CQRS.Handlers.GetPublicBlogBySlugQueryHandler(db, new HtmlContentSanitizer());
        var publicPosts = await list.Handle(new BusinessLayer.CQRS.Queries.GetPublicBlogsQuery(), default);
        Assert.Equal(published, Assert.Single(publicPosts.Items).Id);
        foreach (var id in new[] { draft, pending, rejected, deleted })
            Assert.Null(await detail.Handle(new BusinessLayer.CQRS.Queries.GetPublicBlogBySlugQuery("post-" + id), default));
        Assert.NotNull(await detail.Handle(new BusinessLayer.CQRS.Queries.GetPublicBlogBySlugQuery("post-" + published), default));
    }

    public void Dispose() { db.Dispose(); connection.Dispose(); provider.Dispose(); }
}
