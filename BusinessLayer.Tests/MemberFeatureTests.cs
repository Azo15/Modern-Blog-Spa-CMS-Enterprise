using System.Reflection;
using System.Text.Json;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebUI.Controllers;
using BusinessLayer;
using Microsoft.Extensions.Localization;
using Moq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebUI.Models;
using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessLayer.Tests;

public sealed class MemberFeatureTests
{
    [Fact]
    public async Task FavoriteToggleCreatesOneUtcRecordAndWithdrawsIt()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var added = await f.Sender.Send(new ToggleBlogFavoriteCommand(1));
        Assert.True(added.IsFavorite);
        var favorite = await f.Db.Set<BlogFavorite>().SingleAsync();
        Assert.Equal(2, favorite.AppUserId); Assert.Equal(DateTimeKind.Utc, favorite.CreatedDate.Kind);
        Assert.False((await f.Sender.Send(new ToggleBlogFavoriteCommand(1))).IsFavorite);
        Assert.Empty(await f.Db.Set<BlogFavorite>().ToListAsync());
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateFavoriteEvenWhenTwoContextsObservedNoFavorite()
    {
        using var f = new Stage2Fixture();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite((SqliteConnection)f.Db.Database.GetDbConnection()).Options;
        using var first = new AppDbContext(options); using var second = new AppDbContext(options);
        Assert.False(await first.Set<BlogFavorite>().AnyAsync()); Assert.False(await second.Set<BlogFavorite>().AnyAsync());
        first.Add(new BlogFavorite { AppUserId = 2, BlogId = 1 });
        second.Add(new BlogFavorite { AppUserId = 2, BlogId = 1 });
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        Assert.Single(await f.Db.Set<BlogFavorite>().AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task ReactionReplacesPreviousTypeAndSameTypeTogglesOff()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var like = await f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Like));
        Assert.Equal(ReactionType.Like, like.Reaction); Assert.Equal(1, like.LikeCount);
        var heart = await f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Heart));
        Assert.Equal(ReactionType.Heart, heart.Reaction); Assert.Equal(0, heart.LikeCount); Assert.Equal(1, heart.HeartCount);
        Assert.Single(await f.Db.Set<BlogReaction>().ToListAsync());
        Assert.Null((await f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Heart))).Reaction);
        Assert.Empty(await f.Db.Set<BlogReaction>().ToListAsync());
    }

    [Fact]
    public async Task ExplicitReactionWithdrawalIsIdempotent()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        await f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Clap));
        await f.Sender.Send(new SetBlogReactionCommand(1, null));
        var second = await f.Sender.Send(new SetBlogReactionCommand(1, null));
        Assert.Null(second.Reaction); Assert.Equal(0, second.ClapCount);
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateAndUnknownReactionTypes()
    {
        using var f = new Stage2Fixture();
        f.Db.Add(new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Like }); await f.Db.SaveChangesAsync();
        f.Db.Add(new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Heart });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
        f.Db.Add(new BlogReaction { AppUserId = 3, BlogId = 1, ReactionType = (ReactionType)99 });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(PublishStatus.Draft, false)]
    [InlineData(PublishStatus.PendingReview, false)]
    [InlineData(PublishStatus.Rejected, false)]
    [InlineData(PublishStatus.Published, true)]
    public async Task UnpublishedOrDeletedBlogsCannotReceiveInteractions(PublishStatus status, bool deleted)
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var blog = await f.Db.Blogs.SingleAsync(); blog.PublishStatus = status; blog.IsDeleted = deleted; await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new ToggleBlogFavoriteCommand(1)));
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Like)));
        Assert.Empty(await f.Db.Set<BlogFavorite>().ToListAsync()); Assert.Empty(await f.Db.Set<BlogReaction>().ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistingCookieCannotCommentOrInteractAfterDatabaseAccountBlock(bool lockout)
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var comment = await f.Sender.Send(new CreateCommentCommand(1, "Before block"));
        var user = await f.Db.Users.SingleAsync(u => u.Id == 2);
        if (lockout) { user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); }
        else user.IsActive = false;
        await f.Db.SaveChangesAsync();
        Assert.True(f.User.User.Identity!.IsAuthenticated);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new CreateCommentCommand(1, "Blocked comment")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new UpdateCommentCommand(comment, "Blocked update")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new ToggleBlogFavoriteCommand(1)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Like)));
        Assert.Equal("Before block", (await f.Db.Comments.SingleAsync()).Content);
    }

    [Fact]
    public async Task ExpiredLockoutDoesNotPreventInteractions()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var user = await f.Db.Users.SingleAsync(u => u.Id == 2); user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1); await f.Db.SaveChangesAsync();
        Assert.True((await f.Sender.Send(new ToggleBlogFavoriteCommand(1))).IsFavorite);
    }

    [Fact]
    public async Task AnonymousCannotMutateOrAccessAnotherUsersFavorites()
    {
        using var f = new Stage2Fixture(); f.As(null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new ToggleBlogFavoriteCommand(1)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Like)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new GetUserFavoritesQuery()));
    }

    [Fact]
    public async Task FavoritesArePaginatedOwnedAndHideUnpublishedBlogs()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        f.Db.Blogs.AddRange(new Blog { Id = 2, AppUserId = 1, Title = "Draft", PublishStatus = PublishStatus.Draft },
            new Blog { Id = 3, AppUserId = 1, Title = "Third", PublishStatus = PublishStatus.Published });
        await f.Db.SaveChangesAsync();
        f.Db.AddRange(new BlogFavorite { AppUserId = 2, BlogId = 1 }, new BlogFavorite { AppUserId = 2, BlogId = 2 },
            new BlogFavorite { AppUserId = 2, BlogId = 3 }, new BlogFavorite { AppUserId = 3, BlogId = 1 }); await f.Db.SaveChangesAsync();
        var page = await f.Sender.Send(new GetUserFavoritesQuery(1, 1));
        Assert.Single(page.Items); Assert.Equal(2, page.TotalPages); Assert.True(page.HasNextPage); Assert.False(page.HasPreviousPage);
        var last = await f.Sender.Send(new GetUserFavoritesQuery(2, 1)); Assert.Single(last.Items); Assert.True(last.HasPreviousPage);
        Assert.DoesNotContain(2, page.Items.Concat(last.Items).Select(x => x.BlogId));
        f.As(AppRoles.Staff, 3); Assert.Single((await f.Sender.Send(new GetUserFavoritesQuery())).Items);
    }

    [Fact]
    public async Task PublicCountsExposeNoUserIdentifiersOrIdentitySecrets()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        await f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Clap)); f.As(null);
        var counts = await f.Sender.Send(new GetBlogInteractionQuery(1));
        Assert.Equal(1, counts.ClapCount); Assert.Null(counts.Reaction); Assert.False(counts.IsFavorite);
        var json = JsonSerializer.Serialize(counts); Assert.DoesNotContain("AppUserId", json); Assert.DoesNotContain("Email", json); Assert.DoesNotContain("private", json);
    }

    [Fact]
    public async Task ModeratorLocksAndUnlocksWithoutChangingRolesAndExistingMemberIsBlocked()
    {
        using var f = new Stage2Fixture();
        var originalStamp = (await f.Db.Users.SingleAsync(u => u.Id == 2)).SecurityStamp;
        f.As(AppRoles.Staff, 3); Assert.True(await f.Sender.Send(new SetAccountLockoutCommand(2, true, 7)));
        var user = await f.Db.Users.SingleAsync(u => u.Id == 2); Assert.True(user.LockoutEnabled); Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow); Assert.NotEqual(originalStamp, user.SecurityStamp);
        Assert.Empty(await f.Db.UserRoles.ToListAsync());
        f.As(AppRoles.Customer); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new ToggleBlogFavoriteCommand(1)));
        f.As(AppRoles.Staff, 3); Assert.True(await f.Sender.Send(new SetAccountLockoutCommand(2, false))); Assert.Null(user.LockoutEnd);
        f.As(AppRoles.Customer); Assert.True((await f.Sender.Send(new ToggleBlogFavoriteCommand(1))).IsFavorite);
    }

    [Fact]
    public async Task ModeratorCannotLockAdminOrSelfAndHasNoRoleManagementPolicy()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Staff, 3);
        f.Db.Roles.Add(new IdentityRole<int>(AppRoles.Admin) { Id = 1, NormalizedName = AppRoles.Admin.ToUpperInvariant() });
        f.Db.UserRoles.Add(new IdentityUserRole<int> { UserId = 1, RoleId = 1 }); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new SetAccountLockoutCommand(1, true)));
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new SetAccountLockoutCommand(3, true)));
        var accounts = await f.Sender.Send(new GetModerationAccountsQuery());
        Assert.False(accounts.Items.Single(a => a.Id == 1).CanManage); Assert.False(accounts.Items.Single(a => a.Id == 3).CanManage);
        Assert.Equal(AccessPolicies.Admin, typeof(AdminUserRoleController).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
    }

    [Theory]
    [InlineData(AppRoles.Staff)]
    [InlineData(AppRoles.Staff)]
    [InlineData(AppRoles.Customer)]
    [InlineData(null)]
    public async Task NonModeratorsCannotListOrLockAccounts(string? role)
    {
        using var f = new Stage2Fixture(); f.As(role);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new SetAccountLockoutCommand(1, true)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new GetModerationAccountsQuery()));
    }

    [Fact]
    public async Task LockedModeratorCannotApproveOrLockOtherMembers()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer); var comment = await f.Sender.Send(new CreateCommentCommand(1, "Pending"));
        var moderator = await f.Db.Users.SingleAsync(u => u.Id == 3); moderator.LockoutEnabled = true; moderator.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); await f.Db.SaveChangesAsync();
        f.As(AppRoles.Staff, 3);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new ApproveCommentCommand(comment)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sender.Send(new SetAccountLockoutCommand(2, true)));
    }

    [Fact]
    public async Task AdministratorCanLockModeratorWithoutRemovingIdentityRoles()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Admin, 1);
        f.Db.Roles.Add(new IdentityRole<int>(AppRoles.Staff) { Id = 10 });
        f.Db.UserRoles.Add(new IdentityUserRole<int> { UserId = 3, RoleId = 10 }); await f.Db.SaveChangesAsync();
        Assert.True(await f.Sender.Send(new SetAccountLockoutCommand(3, true, 2)));
        Assert.True((await f.Db.Users.SingleAsync(u => u.Id == 3)).LockoutEnabled);
        var link = await f.Db.UserRoles.SingleAsync(); Assert.Equal(3, link.UserId); Assert.Equal(10, link.RoleId);
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new SetAccountLockoutCommand(1, true)));
    }

    [Fact]
    public async Task UnlockDoesNotRequireAPositiveLockDurationAndMissingTargetReturnsFalse()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Staff, 3);
        await f.Sender.Send(new SetAccountLockoutCommand(2, true));
        Assert.True(await f.Sender.Send(new SetAccountLockoutCommand(2, false, 0)));
        Assert.Null((await f.Db.Users.SingleAsync(u => u.Id == 2)).LockoutEnd);
        Assert.False(await f.Sender.Send(new SetAccountLockoutCommand(99, true)));
    }

    [Fact]
    public async Task SoftDeletedBlogKeepsInteractionsHiddenUntilRestore()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        await f.Sender.Send(new ToggleBlogFavoriteCommand(1));
        await f.Sender.Send(new SetBlogReactionCommand(1, ReactionType.Like));
        var blog = await f.Db.Blogs.SingleAsync(); f.Db.Remove(blog); await f.Db.SaveChangesAsync();
        Assert.Single(await f.Db.Set<BlogFavorite>().ToListAsync()); Assert.Single(await f.Db.Set<BlogReaction>().ToListAsync());
        Assert.Empty((await f.Sender.Send(new GetUserFavoritesQuery())).Items);
        Assert.Equal(0, (await f.Sender.Send(new GetBlogInteractionQuery(1))).LikeCount);
        blog.IsDeleted = false; await f.Db.SaveChangesAsync();
        Assert.Single((await f.Sender.Send(new GetUserFavoritesQuery())).Items);
        Assert.Equal(1, (await f.Sender.Send(new GetBlogInteractionQuery(1))).LikeCount);
    }

    [Theory]
    [InlineData("2", true, true)]
    [InlineData("999", true, false)]
    [InlineData("invalid", true, false)]
    [InlineData(null, true, false)]
    [InlineData("2", false, false)]
    public async Task ActivePolicyRequiresARealAuthenticatedDatabaseUser(string? id, bool authenticated, bool expected)
    {
        using var f = new Stage2Fixture();
        var principal = AuthorizationTests.Principal(AppRoles.Customer, id, authenticated);
        var service = new ActiveMemberService(f.Db, f.User, Mock.Of<IStringLocalizer<SharedResource>>());
        var handler = new ActiveMemberAuthorizationHandler(service);
        var context = new AuthorizationHandlerContext([new ActiveMemberRequirement()], principal, null);
        await handler.HandleAsync(context);
        Assert.Equal(expected, context.HasSucceeded);
        var user = await f.Db.Users.SingleAsync(u => u.Id == 2); user.LockoutEnabled = true; user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1); await f.Db.SaveChangesAsync();
        context = new AuthorizationHandlerContext([new ActiveMemberRequirement()], principal, null);
        await handler.HandleAsync(context);
        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task BlockedAccountCannotCreateANewSessionOrIncrementPasswordFailureCounters(bool locked, bool inactive)
    {
        var user = new AppUser { Id = 2, UserName = "user@spa.com", Email = "user@spa.com", IsActive = !inactive, FailedLoginCount = 2 };
        var users = UserManagerMock();
        users.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        users.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(locked);
        var signIn = new Mock<SignInManager<AppUser>>(users.Object, Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(), Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<AppUser>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<AppUser>>());
        var text = new Mock<IStringLocalizer<SharedResource>>();
        text.Setup(x => x["AccountDisabled"]).Returns(new LocalizedString("AccountDisabled", "Account is disabled."));
        var controller = new DashboardController(users.Object, signIn.Object, text.Object);
        Assert.IsType<ViewResult>(await controller.SignIn(new SignInViewModel { Email = user.Email, Password = "P@ssword123!" }));
        Assert.False(controller.ModelState.IsValid); Assert.Equal(2, user.FailedLoginCount);
        signIn.Verify(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        users.Verify(x => x.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Theory]
    [InlineData("Favorite")]
    [InlineData("Reaction")]
    [InlineData("DeletedComment")]
    public async Task LegacyAdminDeletionPreservesAccountsWithInteractionOrHiddenCommentHistory(string history)
    {
        using var f = new Stage2Fixture();
        if (history == "Favorite") f.Db.Add(new BlogFavorite { AppUserId = 2, BlogId = 1 });
        else if (history == "Reaction") f.Db.Add(new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Like });
        else f.Db.Comments.Add(new Comment { BlogId = 1, AppUserId = 2, Content = "Retained audit history" });
        await f.Db.SaveChangesAsync();
        if (history == "DeletedComment") { f.Db.Remove(await f.Db.Comments.SingleAsync()); await f.Db.SaveChangesAsync(); }
        var target = await f.Db.Users.SingleAsync(u => u.Id == 2);
        var users = UserManagerMock();
        users.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(await f.Db.Users.SingleAsync(u => u.Id == 1));
        users.Setup(x => x.FindByIdAsync("2")).ReturnsAsync(target);
        var files = new Mock<IFileHelper>();
        var text = new Mock<IStringLocalizer<SharedResource>>();
        text.Setup(x => x["AccountHasHistory"]).Returns(new LocalizedString("AccountHasHistory", "Use account moderation."));
        var controller = new AdminUserListController(users.Object, files.Object, f.Db, text.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = AuthorizationTests.Principal(AppRoles.Admin) } },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        Assert.IsType<RedirectToActionResult>(await controller.Delete(2));
        Assert.Equal("warning", controller.TempData["ToastType"]);
        Assert.Equal("Use account moderation.", controller.TempData["ToastMessage"]);
        users.Verify(x => x.DeleteAsync(It.IsAny<AppUser>()), Times.Never); files.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
        Assert.Equal(EntityState.Unchanged, f.Db.Entry(target).State);
        Assert.True(await f.Db.Users.AnyAsync(u => u.Id == 2));
        Assert.Equal(history == "Favorite" ? 1 : 0, await f.Db.BlogFavorites.CountAsync());
        Assert.Equal(history == "Reaction" ? 1 : 0, await f.Db.BlogReactions.CountAsync());
        Assert.Equal(history == "DeletedComment" ? 1 : 0, await f.Db.Comments.IgnoreQueryFilters().CountAsync());
    }

    private static Mock<UserManager<AppUser>> UserManagerMock()
        => new(Mock.Of<IUserStore<AppUser>>(), Options.Create(new IdentityOptions()), Mock.Of<IPasswordHasher<AppUser>>(),
            Array.Empty<IUserValidator<AppUser>>(), Array.Empty<IPasswordValidator<AppUser>>(), Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), NullLogger<UserManager<AppUser>>.Instance);

    [Fact]
    public async Task ValidationRejectsInvalidReactionAndPaginationBeforePersistence()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new SetBlogReactionCommand(1, (ReactionType)99)));
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new GetUserFavoritesQuery(0, 101)));
        f.As(AppRoles.Staff, 3); await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new SetAccountLockoutCommand(2, true, 0)));
    }

    [Fact]
    public void MemberControllersRequireActivePolicyAndEveryMutationUsesAntiforgery()
    {
        Assert.Equal(AccessPolicies.ActiveMember, typeof(ProfileController).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        Assert.Equal(AccessPolicies.ActiveMember, typeof(BlogInteractionController).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        Assert.Equal(AccessPolicies.AccountModerator, typeof(AdminAccountController).GetCustomAttribute<AuthorizeAttribute>()!.Policy);
        foreach (var type in new[] { typeof(ProfileController), typeof(BlogInteractionController), typeof(AdminAccountController) })
        foreach (var action in type.GetMethods().Where(m => m.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPostAttribute>() is not null))
            Assert.NotNull(action.GetCustomAttribute<Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute>());
    }
}
