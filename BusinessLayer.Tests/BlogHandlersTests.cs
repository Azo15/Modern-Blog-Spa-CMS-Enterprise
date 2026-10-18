using System.Text.Json;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace BusinessLayer.Tests;
public class BlogHandlersTests
{
    [Fact]
    public async Task PublicQueryPagesInDatabaseAndCachesOnlyReadOnlyDTOs()
    {
        using var f = new Stage2Fixture(true);
        f.Db.Blogs.AddRange(Enumerable.Range(2, 6).Select(id => new Blog { Id = id, Title = "Blog " + id, AppUserId = 1, CatagoryId = 1, PublishStatus = id <= 5 ? PublishStatus.Published : PublishStatus.Draft })); await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        var result = await f.Sender.Send(new GetPublicBlogsQuery(2, 2));
        Assert.Equal(5, result.TotalCount); Assert.Equal(3, result.TotalPages); Assert.Equal(2, result.Items.Count); Assert.True(result.HasPreviousPage); Assert.True(result.HasNextPage);
        Assert.Empty(f.Db.ChangeTracker.Entries());
        var json = JsonSerializer.Serialize(result);
        foreach (var secret in new[] { "private-hash", "private-stamp", "@example.com", "PasswordHash", "SecurityStamp", "CreatedBy", "AppUserId" }) Assert.DoesNotContain(secret, json);
        f.Cache.Verify(c => c.SetAsync(It.Is<string>(k => k.StartsWith("blogs_all:")), It.IsAny<BusinessLayer.Dtos.PaginatedList<BusinessLayer.Dtos.BlogListDto>>(), It.IsAny<TimeSpan?>()), Times.Once);
    }
    [Fact]
    public async Task StaffQueriesAllAuthorsWhileCustomersCannotAccessManagement()
    {
        using var f = new Stage2Fixture(); f.Db.Blogs.Add(new Blog { Id = 2, AppUserId = 6, Title = "Other author" }); await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        var owned = await f.Sender.Send(new GetBlogsQuery()); Assert.Equal(2, owned.Items.Count);
        Assert.NotNull(await f.Sender.Send(new GetBlogByIdQuery(2)));
        f.As(AppRoles.Customer); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new GetBlogsQuery()));
    }
    [Fact]
    public async Task CategoryChangeInvalidatesBothCategoriesAndAllListPages()
    {
        using var f = new Stage2Fixture(true); f.As(AppRoles.Staff, 5);
        Assert.True(await f.Sender.Send(new UpdateBlogCommand(1, "Updated title", "Updated subtitle", "<p>Updated</p>", 2)));
        foreach (var prefix in new[] { "blogs_all", "blogs_cat_1", "blogs_cat_2", "blog_detail_1" }) f.Cache.Verify(c => c.RemoveByPrefixAsync(prefix), Times.AtLeastOnce);
        Assert.Equal(1, (await f.Db.Blogs.SingleAsync()).AppUserId);
    }
    [Fact]
    public async Task PipelineRejectsInvalidCommandAndPaginationBeforeWrites()
    {
        using var f = new Stage2Fixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateBlogCommand("", "short", "", 0)));
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new GetPublicBlogsQuery(0, 101)));
        Assert.Equal(1, await f.Db.Blogs.CountAsync());
    }
    [Fact]
    public async Task CommandsKeepDraftWorkflowAndDeleteRestoreRules()
    {
        using var f = new Stage2Fixture(true);
        var id = await f.Sender.Send(new CreateBlogCommand("New blog title", "A valid subtitle", "<p>Content</p>", 1));
        Assert.Equal(PublishStatus.Draft, (await f.Sender.Send(new GetBlogByIdQuery(id)))!.PublishStatus);
        f.As(AppRoles.Customer);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new ChangeBlogStatusCommand(id, PublishStatus.Published)));
        f.As(AppRoles.Staff);
        await f.Sender.Send(new ChangeBlogStatusCommand(id, PublishStatus.PendingReview));
        f.As(AppRoles.Staff, 5); await f.Sender.Send(new ChangeBlogStatusCommand(id, PublishStatus.Published)); await f.Sender.Send(new DeleteBlogCommand(id));
        Assert.Equal(id, Assert.Single((await f.Sender.Send(new GetTrashedBlogsQuery())).Items).Id);
        await f.Sender.Send(new RestoreBlogCommand(id)); Assert.Equal(PublishStatus.Draft, (await f.Sender.Send(new GetBlogByIdQuery(id)))!.PublishStatus);
        f.Cache.Verify(c => c.RemoveByPrefixAsync("blogs_all"), Times.AtLeast(5));
    }
    [Fact]
    public async Task CachedPublicDataCannotResurrectADeletedOrUnpublishedBlog()
    {
        using var f = new Stage2Fixture(); f.As(null);
        Assert.Single((await f.Sender.Send(new GetPublicBlogsQuery())).Items);
        f.As(AppRoles.Staff, 5); await f.Sender.Send(new ChangeBlogStatusCommand(1, PublishStatus.Draft));
        f.As(null); Assert.Empty((await f.Sender.Send(new GetPublicBlogsQuery())).Items); Assert.Null(await f.Sender.Send(new GetPublicBlogBySlugQuery("published-title")));
    }
}
