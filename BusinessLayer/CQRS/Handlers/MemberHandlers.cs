using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.CQRS.Handlers;

internal static class InteractionProjection
{
    public static async Task<BlogInteractionDto> Read(AppDbContext db, int blogId, int? userId, CancellationToken ct)
    {
        var reactions = await db.Set<BlogReaction>().AsNoTracking().Where(r => r.BlogId == blogId)
            .GroupBy(r => r.ReactionType).Select(g => new { Type = g.Key, Count = g.Count() }).ToListAsync(ct);
        var own = userId.HasValue ? await db.Set<BlogReaction>().AsNoTracking().Where(r => r.BlogId == blogId && r.AppUserId == userId)
            .Select(r => (ReactionType?)r.ReactionType).SingleOrDefaultAsync(ct) : null;
        return new(userId.HasValue && await db.Set<BlogFavorite>().AnyAsync(f => f.BlogId == blogId && f.AppUserId == userId, ct), own,
            reactions.Where(r => r.Type == ReactionType.Like).Sum(r => r.Count),
            reactions.Where(r => r.Type == ReactionType.Heart).Sum(r => r.Count),
            reactions.Where(r => r.Type == ReactionType.Clap).Sum(r => r.Count));
    }
    public static Task<bool> IsPublished(AppDbContext db, int id, CancellationToken ct)
        => db.Blogs.AnyAsync(b => b.Id == id && !b.IsDeleted && b.PublishStatus == PublishStatus.Published, ct);
}

public sealed class MemberInteractionHandlers(AppDbContext db, IActiveMemberService members) :
    IRequestHandler<ToggleBlogFavoriteCommand, BlogInteractionDto>, IRequestHandler<SetBlogReactionCommand, BlogInteractionDto>,
    IRequestHandler<GetUserFavoritesQuery, PaginatedList<FavoriteBlogDto>>
{
    private async Task<int> RequirePublished(int blogId, CancellationToken ct)
    {
        var id = await members.RequireAsync(ct);
        if (!await InteractionProjection.IsPublished(db, blogId, ct)) throw new ValidationException("Blog yayında değil veya bulunamadı.");
        return id;
    }

    public async Task<BlogInteractionDto> Handle(ToggleBlogFavoriteCommand r, CancellationToken ct)
    {
        var id = await RequirePublished(r.BlogId, ct);
        var existing = await db.Set<BlogFavorite>().SingleOrDefaultAsync(f => f.AppUserId == id && f.BlogId == r.BlogId, ct);
        if (existing is not null)
        {
            db.Remove(existing);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { db.Entry(existing).State = EntityState.Detached; }
        }
        else
        {
            var favorite = new BlogFavorite { AppUserId = id, BlogId = r.BlogId, CreatedDate = DateTime.UtcNow };
            db.Add(favorite);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                // A parallel add may win the unique index. Keep the original add intent.
                db.Entry(favorite).State = EntityState.Detached;
                if (!await db.Set<BlogFavorite>().AnyAsync(f => f.AppUserId == id && f.BlogId == r.BlogId, ct)) throw;
            }
        }
        return await InteractionProjection.Read(db, r.BlogId, id, ct);
    }

    public async Task<BlogInteractionDto> Handle(SetBlogReactionCommand r, CancellationToken ct)
    {
        var id = await RequirePublished(r.BlogId, ct);
        if (r.ReactionType.HasValue && !System.Enum.IsDefined(r.ReactionType.Value)) throw new ValidationException("Geçersiz tepki.");
        var existing = await db.Set<BlogReaction>().SingleOrDefaultAsync(x => x.AppUserId == id && x.BlogId == r.BlogId, ct);
        if (existing is not null && (r.ReactionType is null || existing.ReactionType == r.ReactionType)) db.Remove(existing);
        else if (existing is not null) existing.ReactionType = r.ReactionType!.Value;
        else if (r.ReactionType.HasValue)
        {
            var created = new BlogReaction { AppUserId = id, BlogId = r.BlogId, ReactionType = r.ReactionType.Value, CreatedDate = DateTime.UtcNow };
            db.Add(created);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException)
            {
                db.Entry(created).State = EntityState.Detached;
                var concurrent = await db.Set<BlogReaction>().SingleOrDefaultAsync(x => x.AppUserId == id && x.BlogId == r.BlogId, ct);
                if (concurrent is null) throw;
                concurrent.ReactionType = r.ReactionType.Value;
                await db.SaveChangesAsync(ct);
            }
            return await InteractionProjection.Read(db, r.BlogId, id, ct);
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) when (existing is not null && db.Entry(existing).State == EntityState.Deleted)
        { db.Entry(existing).State = EntityState.Detached; }
        return await InteractionProjection.Read(db, r.BlogId, id, ct);
    }

    public async Task<PaginatedList<FavoriteBlogDto>> Handle(GetUserFavoritesQuery r, CancellationToken ct)
    {
        var id = await members.RequireAsync(ct);
        var query = db.Set<BlogFavorite>().AsNoTracking().Where(f => f.AppUserId == id && !f.Blog.IsDeleted && f.Blog.PublishStatus == PublishStatus.Published)
            .OrderByDescending(f => f.CreatedDate).ThenByDescending(f => f.Id)
            .Select(f => new FavoriteBlogDto(f.BlogId, f.Blog.Title, f.Blog.Slug, f.Blog.Image, f.CreatedDate));
        return await BlogProjection.Page(query, r.PageIndex, r.PageSize, ct);
    }
}

public sealed class BlogInteractionQueryHandler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<GetBlogInteractionQuery, BlogInteractionDto>
{
    public async Task<BlogInteractionDto> Handle(GetBlogInteractionQuery r, CancellationToken ct)
    {
        if (!await InteractionProjection.IsPublished(db, r.BlogId, ct)) return new(false, null, 0, 0, 0);
        int? id = int.TryParse(currentUser.UserId, out var userId) ? userId : null;
        return await InteractionProjection.Read(db, r.BlogId, id, ct);
    }
}

public sealed class AccountModerationHandlers(AppDbContext db, ICurrentUserService currentUser, IActiveMemberService members, IAuthorizationService authorization) :
    IRequestHandler<SetAccountLockoutCommand, bool>, IRequestHandler<GetModerationAccountsQuery, PaginatedList<ModerationAccountDto>>
{
    private async Task<int> RequireModerator(CancellationToken ct)
    {
        var id = await members.RequireAsync(ct);
        if (!(await authorization.AuthorizeAsync(currentUser.User, AccessPolicies.AccountModerator)).Succeeded) throw new UnauthorizedAccessException();
        return id;
    }
    private IQueryable<int> AdminIds => from link in db.UserRoles join role in db.Roles on link.RoleId equals role.Id where role.Name == AppRoles.Admin select link.UserId;

    public async Task<bool> Handle(SetAccountLockoutCommand r, CancellationToken ct)
    {
        var ownId = await RequireModerator(ct);
        if (r.UserId == ownId) throw new ValidationException("Kendi hesabınızı engelleyemezsiniz.");
        if (!currentUser.User.IsInRole(AppRoles.Admin) && await AdminIds.ContainsAsync(r.UserId, ct)) throw new UnauthorizedAccessException();
        var account = await db.Users.SingleOrDefaultAsync(u => u.Id == r.UserId, ct);
        if (account is null) return false;
        account.LockoutEnabled = true;
        account.LockoutEnd = r.IsLocked ? DateTimeOffset.UtcNow.AddDays(r.Days) : null;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PaginatedList<ModerationAccountDto>> Handle(GetModerationAccountsQuery r, CancellationToken ct)
    {
        var ownId = await RequireModerator(ct);
        var admin = currentUser.User.IsInRole(AppRoles.Admin);
        var accounts = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(r.Search))
        { var term = r.Search.Trim(); accounts = accounts.Where(u => (u.Email != null && u.Email.Contains(term)) || (u.FirstName != null && u.FirstName.Contains(term)) || (u.LastName != null && u.LastName.Contains(term))); }
        var now = DateTimeOffset.UtcNow;
        // Lockout is derived after materialization: SQLite does not compare DateTimeOffset SQL values.
        var count = await accounts.CountAsync(ct);
        var page = await accounts.OrderBy(u => u.Id).Skip((r.PageIndex - 1) * r.PageSize).Take(r.PageSize)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.LockoutEnabled, u.LockoutEnd, IsAdmin = AdminIds.Contains(u.Id) }).ToListAsync(ct);
        return new(page.Select(u => new ModerationAccountDto(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.Email,
            u.LockoutEnabled && u.LockoutEnd > now, u.LockoutEnd, u.Id != ownId && (admin || !u.IsAdmin))).ToArray(), count, r.PageIndex, r.PageSize);
    }
}
