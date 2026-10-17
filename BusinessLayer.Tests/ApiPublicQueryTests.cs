using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BusinessLayer.Tests;

public class ApiPublicQueryTests
{
    [Fact]
    public async Task ReactionTotalsRemainCurrentWhenArticleMetadataIsCached()
    {
        using var f = new Stage2Fixture();
        f.As(null);
        var initial = await f.Sender.Send(new GetPublicBlogsQuery());
        Assert.Equal(0, Assert.Single(initial.Items).LikeCount);
        f.Db.BlogReactions.AddRange(
            new BlogReaction { BlogId = 1, AppUserId = 2, ReactionType = ReactionType.Like },
            new BlogReaction { BlogId = 1, AppUserId = 3, ReactionType = ReactionType.Heart });
        await f.Db.SaveChangesAsync();
        var afterReaction = Assert.Single((await f.Sender.Send(new GetPublicBlogsQuery())).Items);
        Assert.Equal(1, afterReaction.LikeCount);
        Assert.Equal(1, afterReaction.HeartCount);
        f.Db.BlogReactions.Remove(await f.Db.BlogReactions.SingleAsync(r => r.AppUserId == 2));
        await f.Db.SaveChangesAsync();
        Assert.Equal(0, Assert.Single((await f.Sender.Send(new GetPublicBlogsQuery())).Items).LikeCount);
    }

    [Fact]
    public async Task SearchAndCategoryFiltersApplyBeforePaginationAndExcludePrivateContent()
    {
        using var f = new Stage2Fixture();
        f.As(null);
        f.Db.Blogs.AddRange(
            new Blog { Id = 2, Title = "Needle published", SubTitle = "Public", CatagoryId = 2, AppUserId = 1, PublishStatus = PublishStatus.Published },
            new Blog { Id = 3, Title = "Needle draft", CatagoryId = 1, AppUserId = 1, PublishStatus = PublishStatus.Draft },
            new Blog { Id = 4, Title = "Needle deleted", CatagoryId = 1, AppUserId = 1, PublishStatus = PublishStatus.Published, IsDeleted = true },
            new Blog { Id = 5, Title = "Other published", Content = "A content Needle to find", CatagoryId = 1, AppUserId = 1, PublishStatus = PublishStatus.Published });
        await f.Db.SaveChangesAsync();
        f.Db.Remove(await f.Db.Blogs.SingleAsync(b => b.Id == 4));
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var found = await f.Sender.Send(new GetPublicBlogsQuery(1, 1, 1, Search: " Needle "));
        Assert.Equal(1, found.TotalCount);
        Assert.Equal(5, Assert.Single(found.Items).Id);
        Assert.Empty(f.Db.ChangeTracker.Entries());
        Assert.Equal(2, (await f.Sender.Send(new GetPublicBlogsQuery(Search: "Needle"))).TotalCount);
    }

    [Fact]
    public async Task SearchCacheKeysCannotReuseDifferentSearchesOrExposeRawSearchText()
    {
        using var f = new Stage2Fixture(true);
        f.As(null);
        await f.Sender.Send(new GetPublicBlogsQuery(Search: "public-search-one"));
        await f.Sender.Send(new GetPublicBlogsQuery(Search: "public-search-two"));
        var keys = f.Cache.Invocations.Where(x => x.Method.Name == "SetAsync").Select(x => Assert.IsType<string>(x.Arguments[0])).ToArray();
        Assert.Equal(2, keys.Length);
        Assert.NotEqual(keys[0], keys[1]);
        Assert.All(keys, key => Assert.DoesNotContain("public-search-", key));
    }

    [Fact]
    public async Task SearchIsLiteralAndRejectsExcessivelyLongQueries()
    {
        using var f = new Stage2Fixture();
        f.As(null);
        Assert.Empty((await f.Sender.Send(new GetPublicBlogsQuery(Search: "%' OR 1=1 --"))).Items);
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new GetPublicBlogsQuery(Search: new string('x', 101))));
        await Assert.ThrowsAsync<ValidationException>(() => f.Sender.Send(new GetPublicBlogsQuery(ExcludeId: 0)));
    }

    [Fact]
    public async Task PublicCategoriesCountPublishedVisibleBlogsOnlyAndFilterDeletedCategories()
    {
        using var f = new Stage2Fixture();
        f.As(null);
        f.Db.Catagories.Add(new Catagory { Id = 3, Title = "Deleted category", IsDeleted = true });
        f.Db.Blogs.AddRange(
            new Blog { Id = 2, CatagoryId = 1, AppUserId = 1, PublishStatus = PublishStatus.Draft },
            new Blog { Id = 3, CatagoryId = 1, AppUserId = 1, PublishStatus = PublishStatus.Published, IsDeleted = true },
            new Blog { Id = 4, CatagoryId = 2, AppUserId = 1, PublishStatus = PublishStatus.Published });
        await f.Db.SaveChangesAsync();
        f.Db.Remove(await f.Db.Blogs.SingleAsync(b => b.Id == 3));
        f.Db.Remove(await f.Db.Catagories.SingleAsync(c => c.Id == 3));
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var categories = await f.Sender.Send(new GetPublicCategoriesQuery());
        Assert.Equal(2, categories.Count);
        Assert.All(categories, category => Assert.Equal(1, category.BlogCount));
        Assert.DoesNotContain(categories, category => category.Id == 3);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }
}
