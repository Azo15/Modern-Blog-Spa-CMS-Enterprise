using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using EntityLayer.Enum;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace BusinessLayer.Tests;
public class CommentTests
{
    [Fact]
    public async Task PendingQueueRetainsCommentsWhenTheirBlogIsUnpublished()
    {
        using var fixture = new Stage2Fixture();
        fixture.As(AppRoles.Customer);
        var id = await fixture.Sender.Send(new CreateCommentCommand(1, "Pending comment"));
        fixture.As(AppRoles.Staff, 5);
        await fixture.Sender.Send(new ChangeBlogStatusCommand(1, PublishStatus.Draft));
        fixture.As(AppRoles.Staff, 3);
        Assert.Equal(id, Assert.Single((await fixture.Sender.Send(new GetPendingCommentsQuery())).Items).Id);
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sender.Send(new ApproveCommentCommand(id)));
        Assert.True(await fixture.Sender.Send(new DeleteCommentCommand(id)));
    }
    [Fact]
    public async Task MemberCreatesPendingCommentAndModeratorApprovalMakesItPublic()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var id = await f.Sender.Send(new CreateCommentCommand(1, "Merhaba dünya"));
        var comment = await f.Db.Comments.SingleAsync(); Assert.Equal(2, comment.AppUserId); Assert.False(comment.IsApproved); Assert.Equal("2", comment.CreatedBy); Assert.Equal(DateTimeKind.Utc, comment.CreatedDate.Kind);
        Assert.Empty((await f.Sender.Send(new GetPublicCommentsQuery(1))).Items);
        f.As(AppRoles.Staff, 3); Assert.Equal(id, Assert.Single((await f.Sender.Send(new GetPendingCommentsQuery())).Items).Id);
        Assert.True(await f.Sender.Send(new ApproveCommentCommand(id)));
        f.As(null); Assert.Equal(id, Assert.Single((await f.Sender.Send(new GetPublicCommentsQuery(1))).Items).Comment.Id);
        f.Notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task ThirdLevelReplyAndCrossBlogParentAreRejected()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var root = await f.Sender.Send(new CreateCommentCommand(1, "Root")); f.As(AppRoles.Staff, 3); await f.Sender.Send(new ApproveCommentCommand(root));
        f.As(AppRoles.Customer); var child = await f.Sender.Send(new CreateCommentCommand(1, "Child", root));
        f.As(AppRoles.Staff, 3); await f.Sender.Send(new ApproveCommentCommand(child)); f.As(AppRoles.Customer);
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateCommentCommand(1, "Grandchild", child)));
        f.Db.Blogs.Add(new EntityLayer.Concrete.Blog { Id = 2, Title = "Another", PublishStatus = PublishStatus.Published, AppUserId = 1 }); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateCommentCommand(2, "Wrong parent", root)));
        var thread = Assert.Single((await f.Sender.Send(new GetPublicCommentsQuery(1))).Items); Assert.Equal(child, Assert.Single(thread.Children).Id); Assert.Equal(2, await f.Db.Comments.CountAsync());
    }
    [Theory]
    [InlineData(AppRoles.Staff)]
    [InlineData(AppRoles.Customer)]
    [InlineData(AppRoles.Staff)]
    [InlineData(null)]
    public async Task OnlyAdminOrModeratorCanApprove(string? role)
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer); var id = await f.Sender.Send(new CreateCommentCommand(1, "Needs approval")); f.As(role);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new ApproveCommentCommand(id)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new GetPendingCommentsQuery()));
        Assert.False((await f.Db.Comments.SingleAsync()).IsApproved);
    }
    [Fact]
    public async Task AnonymousCannotCreateAndNonOwnerCannotEditOrDelete()
    {
        using var f = new Stage2Fixture(); f.As(null); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new CreateCommentCommand(1, "Anonymous")));
        f.As(AppRoles.Customer); var id = await f.Sender.Send(new CreateCommentCommand(1, "Owned comment")); f.As(AppRoles.Staff, 6);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new UpdateCommentCommand(id, "Forged edit")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new DeleteCommentCommand(id)));
    }
    [Fact]
    public async Task EditingRootRequiresApprovalAgainAndHidesApprovedChildren()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer); var root = await f.Sender.Send(new CreateCommentCommand(1, "Root")); f.As(AppRoles.Staff, 3); await f.Sender.Send(new ApproveCommentCommand(root));
        f.As(AppRoles.Customer); var child = await f.Sender.Send(new CreateCommentCommand(1, "Child", root)); f.As(AppRoles.Staff, 3); await f.Sender.Send(new ApproveCommentCommand(child));
        f.As(AppRoles.Customer); await f.Sender.Send(new UpdateCommentCommand(root, "Edited root")); Assert.Empty((await f.Sender.Send(new GetPublicCommentsQuery(1))).Items);
        f.As(AppRoles.Staff, 3); await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new ApproveCommentCommand(child))); await f.Sender.Send(new ApproveCommentCommand(root));
        Assert.Single(Assert.Single((await f.Sender.Send(new GetPublicCommentsQuery(1))).Items).Children);
    }
    [Fact]
    public async Task DeletingRootSoftDeletesRepliesAndPreservesAudit()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer); var root = await f.Sender.Send(new CreateCommentCommand(1, "Root")); f.As(AppRoles.Staff, 3); await f.Sender.Send(new ApproveCommentCommand(root));
        f.As(AppRoles.Staff, 6); await f.Sender.Send(new CreateCommentCommand(1, "Reply", root)); f.As(AppRoles.Staff, 3); await f.Sender.Send(new DeleteCommentCommand(root));
        Assert.Empty(await f.Db.Comments.ToListAsync()); var deleted = await f.Db.Comments.IgnoreQueryFilters().ToListAsync(); Assert.Equal(2, deleted.Count); Assert.All(deleted, c => { Assert.True(c.IsDeleted); Assert.Equal("3", c.DeletedBy); Assert.NotNull(c.DeletedDate); });
    }
    [Theory]
    [InlineData("YASAK içerik")]
    [InlineData("")]
    public async Task ProfanityAndEmptyInputCannotBeStored(string content)
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer); await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateCommentCommand(1, content))); Assert.Empty(await f.Db.Comments.ToListAsync());
    }
    [Fact]
    public async Task LongInputUnapprovedParentAndUnpublishedBlogAreRejected()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer); await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateCommentCommand(1, new string('a', 1001))));
        var root = await f.Sender.Send(new CreateCommentCommand(1, "Pending")); await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateCommentCommand(1, "Reply", root)));
        f.As(AppRoles.Staff, 5); await f.Sender.Send(new ChangeBlogStatusCommand(1, PublishStatus.Draft)); f.As(AppRoles.Customer);
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new CreateCommentCommand(1, "Hidden blog"))); Assert.Empty((await f.Sender.Send(new GetPublicCommentsQuery(1))).Items);
    }
}
