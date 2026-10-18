using System.Security.Claims;
using BusinessLayer;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Handlers;
using BusinessLayer.Exceptions;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Moq;

namespace BusinessLayer.Tests;

public sealed class InteractionConcurrencyTests
{
    [Fact]
    public async Task ChangedReactionSurvivesConcurrentWithdrawal()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        f.Db.BlogReactions.Add(new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Like });
        await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        var intercepted = false;
        using var db = Context(f, async (context, ct) =>
        {
            if (intercepted || !context.ChangeTracker.Entries<BlogReaction>().Any(e => e.State == EntityState.Modified)) return;
            intercepted = true;
            await f.Db.BlogReactions.ExecuteDeleteAsync(ct);
        });
        var handler = Handler(db, f);
        var result = await handler.Handle(new SetBlogReactionCommand(1, ReactionType.Heart), default);
        Assert.True(intercepted);
        Assert.Equal(ReactionType.Heart, result.Reaction);
        Assert.Equal(ReactionType.Heart, (await f.Db.BlogReactions.AsNoTracking().SingleAsync()).ReactionType);
    }

    [Fact]
    public async Task DuplicateInsertThenWithdrawalReplaysSetIntent()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var inserted = false; var withdrawn = false;
        using var db = Context(f, async (context, ct) =>
        {
            if (!inserted && context.ChangeTracker.Entries<BlogReaction>().Any(e => e.State == EntityState.Added))
            {
                inserted = true;
                f.Db.BlogReactions.Add(new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Like });
                await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
            }
            else if (!withdrawn && context.ChangeTracker.Entries<BlogReaction>().Any(e => e.State == EntityState.Modified))
            { withdrawn = true; await f.Db.BlogReactions.ExecuteDeleteAsync(ct); }
        });
        var result = await Handler(db, f).Handle(new SetBlogReactionCommand(1, ReactionType.Heart), default);
        Assert.True(inserted); Assert.True(withdrawn);
        Assert.Equal(ReactionType.Heart, result.Reaction);
        Assert.Single(await f.Db.BlogReactions.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task RepeatedReactionRacesReturnControlledConflict()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Customer);
        var races = 0;
        using var db = Context(f, async (context, ct) =>
        {
            await f.Db.BlogReactions.ExecuteDeleteAsync(ct);
            if (context.ChangeTracker.Entries<BlogReaction>().Any(e => e.State == EntityState.Added))
            {
                f.Db.BlogReactions.Add(new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Like });
                await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
            }
            races++;
        });
        await Assert.ThrowsAsync<InteractionConflictException>(() => Handler(db, f).Handle(new SetBlogReactionCommand(1, ReactionType.Heart), default));
        Assert.Equal(3, races);
    }

    [Fact]
    public async Task ConcurrentProfileChangeIsPreservedWhenModeratorLocksAccount()
    {
        using var f = new Stage2Fixture(); f.As(AppRoles.Staff, 3);
        using var db = Context(f, async (_, ct) =>
        {
            await f.Db.Users.Where(u => u.Id == 2).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FirstName, "Concurrent profile")
                .SetProperty(u => u.ConcurrencyStamp, "newer-stamp"), ct);
        });
        var auth = new Mock<IAuthorizationService>();
        auth.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), AccessPolicies.AccountModerator))
            .ReturnsAsync(AuthorizationResult.Success());
        var text = Mock.Of<IStringLocalizer<SharedResource>>();
        var handler = new AccountModerationHandlers(db, f.User, new ActiveMemberService(db, f.User, text), auth.Object, text);
        await Assert.ThrowsAsync<InteractionConflictException>(() => handler.Handle(new SetAccountLockoutCommand(2, true), default));
        var user = await f.Db.Users.AsNoTracking().SingleAsync(u => u.Id == 2);
        Assert.Equal("Concurrent profile", user.FirstName);
        Assert.Equal("newer-stamp", user.ConcurrencyStamp);
        Assert.Null(user.LockoutEnd);
    }

    private static MemberInteractionHandlers Handler(AppDbContext db, Stage2Fixture f)
    {
        var text = Mock.Of<IStringLocalizer<SharedResource>>();
        return new(db, new ActiveMemberService(db, f.User, text), text);
    }

    private static AppDbContext Context(Stage2Fixture f, Func<DbContext, CancellationToken, Task> action)
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(f.Db.Database.GetDbConnection())
            .AddInterceptors(new BeforeSave(action)).Options, f.User);

    private sealed class BeforeSave(Func<DbContext, CancellationToken, Task> action) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { await action(eventData.Context!, cancellationToken); return result; }
    }
}
