using BusinessLayer.Abstract;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public sealed class BlogWorkflowService(AppDbContext db, ICurrentUserService currentUser,
    IAuthorizationService authorization, IHtmlContentSanitizer sanitizer, ICacheService cache) : IBlogWorkflowService
{
    private async Task RequireAsync(string policy, Blog? blog = null)
    {
        if (!(await authorization.AuthorizeAsync(currentUser.User, blog, policy)).Succeeded)
            throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok.");
    }

    private async Task InvalidateAsync(Blog blog, int? oldCategory = null)
    {
        await cache.RemoveAsync("all_blogs_cache");
        await cache.RemoveAsync($"blog_detail_{blog.Id}");
        await cache.RemoveAsync($"blogs_category_{blog.CatagoryId}");
        if (oldCategory != blog.CatagoryId) await cache.RemoveAsync($"blogs_category_{oldCategory}");
    }

    public async Task<int> CreateAsync(Blog blog, CancellationToken cancellationToken = default)
    {
        await RequireAsync(AccessPolicies.BlogAuthor);
        if (!int.TryParse(currentUser.UserId, out var userId)) throw new UnauthorizedAccessException();
        // Never accept author/publish/delete/audit fields supplied by a client.
        var entity = new Blog { Title = blog.Title, SubTitle = blog.SubTitle,
            Content = sanitizer.Sanitize(blog.Content), Slug = blog.Slug, Image = blog.Image,
            CatagoryId = blog.CatagoryId, AppUserId = userId, PublishStatus = PublishStatus.Draft, IsPublish = false };
        db.Blogs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await InvalidateAsync(entity);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, string? title, string? subtitle, string? content, int? categoryId,
        string? image, CancellationToken cancellationToken = default)
    {
        var blog = await db.Blogs.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (blog is null) return false;
        await RequireAsync(AccessPolicies.BlogResource, blog);
        var oldCategory = blog.CatagoryId;
        blog.Title = title; blog.SubTitle = subtitle; blog.Content = sanitizer.Sanitize(content);
        blog.CatagoryId = categoryId;
        // AppUserId is deliberately preserved, including edits by managers.
        if (!string.IsNullOrWhiteSpace(image)) blog.Image = image;
        if (!(await authorization.AuthorizeAsync(currentUser.User, AccessPolicies.ContentManager)).Succeeded)
        {
            blog.PublishStatus = PublishStatus.Draft;
            blog.IsPublish = false;
        }
        await db.SaveChangesAsync(cancellationToken);
        await InvalidateAsync(blog, oldCategory);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var blog = await db.Blogs.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (blog is null) return false;
        await RequireAsync(AccessPolicies.BlogResource, blog);
        db.Blogs.Remove(blog);
        await db.SaveChangesAsync(cancellationToken);
        // Keep the image file for restore; no filesystem deletion here.
        await InvalidateAsync(blog);
        return true;
    }

    public async Task<bool> ChangeStatusAsync(int id, PublishStatus status, CancellationToken cancellationToken = default)
    {
        if (!System.Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        var blog = await db.Blogs.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (blog is null) return false;
        await RequireAsync(AccessPolicies.BlogResource, blog);
        if (status is PublishStatus.Published or PublishStatus.Rejected)
            await RequireAsync(AccessPolicies.ContentManager);
        // Re-sanitize legacy content before it is made public.
        if (status == PublishStatus.Published) blog.Content = sanitizer.Sanitize(blog.Content);
        blog.PublishStatus = status;
        blog.IsPublish = status == PublishStatus.Published;
        await db.SaveChangesAsync(cancellationToken);
        await InvalidateAsync(blog);
        return true;
    }

    public async Task<List<Blog>> GetTrashAsync(CancellationToken cancellationToken = default)
    {
        await RequireAsync(AccessPolicies.ContentManager);
        return await db.Blogs.IgnoreQueryFilters().AsNoTracking().Where(b => b.IsDeleted)
            .OrderByDescending(b => b.DeletedDate).ToListAsync(cancellationToken);
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        await RequireAsync(AccessPolicies.ContentManager);
        var blog = await db.Blogs.IgnoreQueryFilters().SingleOrDefaultAsync(b => b.Id == id && b.IsDeleted, cancellationToken);
        if (blog is null) return false;
        blog.IsDeleted = false; blog.DeletedDate = null; blog.DeletedBy = null;
        // Restore to draft so old content is reviewed before re-publication.
        blog.PublishStatus = PublishStatus.Draft; blog.IsPublish = false;
        await db.SaveChangesAsync(cancellationToken);
        await InvalidateAsync(blog);
        return true;
    }
}
