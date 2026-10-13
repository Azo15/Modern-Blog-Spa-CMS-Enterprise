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
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
namespace BusinessLayer.CQRS.Handlers;

public sealed class CommentCommandHandlers(AppDbContext db, ICurrentUserService user, IAuthorizationService auth, IProfanityFilterService profanity, IManagerNotifier notifier, IActiveMemberService members)
    : IRequestHandler<CreateCommentCommand, int>, IRequestHandler<UpdateCommentCommand, bool>, IRequestHandler<DeleteCommentCommand, bool>, IRequestHandler<ApproveCommentCommand, bool>
{
    private async Task<int> MemberId()
    {
        await members.RequireAsync();
        if (!(await auth.AuthorizeAsync(user.User, AccessPolicies.Member)).Succeeded || !int.TryParse(user.UserId, out var id)) throw new UnauthorizedAccessException();
        return id;
    }
    private static ValidationException Invalid(string message) => new(new[] { new ValidationFailure("Content", message) });
    private string CheckContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000) throw Invalid("Yorum 1–1000 karakter arasında olmalıdır.");
        if (!profanity.IsAllowed(content)) throw Invalid("Yorum uygunsuz kelimeler içeriyor.");
        // Comments are plain text. Razor performs output encoding; no HTML interpretation.
        return content.Trim();
    }
    private async Task RequirePublished(int blogId, CancellationToken ct)
    { if (!await db.Blogs.AnyAsync(b => b.Id == blogId && b.PublishStatus == PublishStatus.Published, ct)) throw Invalid("Blog yayında değil veya bulunamadı."); }
    private async Task RequireModerator()
    { await members.RequireAsync(); if (!(await auth.AuthorizeAsync(user.User, AccessPolicies.CommentModerator)).Succeeded) throw new UnauthorizedAccessException(); }

    public async Task<int> Handle(CreateCommentCommand r, CancellationToken ct)
    {
        var id = await MemberId();
        var content = CheckContent(r.Content);
        await RequirePublished(r.BlogId, ct);
        if (r.ParentCommentId.HasValue)
        {
            var parent = await db.Comments.AsNoTracking().SingleOrDefaultAsync(c => c.Id == r.ParentCommentId, ct);
            if (parent is null || !parent.IsApproved || parent.BlogId != r.BlogId) throw Invalid("Yanıt yalnızca aynı blogdaki onaylı yoruma eklenebilir.");
            if (parent.ParentCommentId.HasValue) throw Invalid("Yorumlarda üçüncü seviyeye izin verilmez.");
        }
        var comment = new Comment { BlogId = r.BlogId, AppUserId = id, ParentCommentId = r.ParentCommentId, Content = content, IsApproved = false };
        db.Comments.Add(comment); await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync("Yeni yorum", $"Yorum #{comment.Id} onay bekliyor.", ct);
        return comment.Id;
    }
    public async Task<bool> Handle(UpdateCommentCommand r, CancellationToken ct)
    {
        var id = await MemberId();
        var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == r.Id, ct);
        if (comment is null) return false;
        if (comment.AppUserId != id) throw new UnauthorizedAccessException();
        await RequirePublished(comment.BlogId, ct);
        comment.Content = CheckContent(r.Content); comment.IsApproved = false;
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync("Yorum güncellendi", $"Yorum #{comment.Id} yeniden onay bekliyor.", ct);
        return true;
    }
    public async Task<bool> Handle(ApproveCommentCommand r, CancellationToken ct)
    {
        await RequireModerator();
        var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == r.Id, ct);
        if (comment is null) return false;
        await RequirePublished(comment.BlogId, ct);
        CheckContent(comment.Content);
        if (comment.ParentCommentId.HasValue && !await db.Comments.AnyAsync(c => c.Id == comment.ParentCommentId && c.IsApproved && c.ParentCommentId == null, ct))
            throw Invalid("Üst yorum onaylı ve görünür olmalıdır.");
        comment.IsApproved = true; await db.SaveChangesAsync(ct); return true;
    }
    public async Task<bool> Handle(DeleteCommentCommand r, CancellationToken ct)
    {
        var id = await MemberId();
        var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == r.Id, ct);
        if (comment is null) return false;
        if (comment.AppUserId != id && !(await auth.AuthorizeAsync(user.User, AccessPolicies.CommentModerator)).Succeeded) throw new UnauthorizedAccessException();
        if (comment.ParentCommentId is null)
            db.Comments.RemoveRange(await db.Comments.Where(c => c.ParentCommentId == comment.Id).ToListAsync(ct));
        db.Comments.Remove(comment); await db.SaveChangesAsync(ct); return true;
    }
}

public sealed class CommentQueryHandlers(AppDbContext db, ICurrentUserService user, IAuthorizationService auth)
    : IRequestHandler<GetPublicCommentsQuery, PaginatedList<CommentThreadDto>>, IRequestHandler<GetPendingCommentsQuery, PaginatedList<PendingCommentDto>>, IRequestHandler<GetMyCommentsQuery, List<OwnCommentDto>>
{
    public async Task<PaginatedList<CommentThreadDto>> Handle(GetPublicCommentsQuery r, CancellationToken ct)
    {
        if (!await db.Blogs.AnyAsync(b => b.Id == r.BlogId && b.PublishStatus == PublishStatus.Published, ct)) return new([], 0, r.PageIndex, r.PageSize);
        var owner = int.TryParse(user.UserId, out var id) ? id : 0;
        var roots = db.Comments.AsNoTracking().Where(c => c.BlogId == r.BlogId && c.ParentCommentId == null && c.IsApproved);
        var count = await roots.CountAsync(ct);
        var rootDtos = await roots.OrderByDescending(c => c.CreatedDate).ThenByDescending(c => c.Id).Skip((r.PageIndex - 1) * r.PageSize).Take(r.PageSize)
            .Select(c => new CommentDto(c.Id, c.ParentCommentId, c.Content, c.AppUser.FirstName + " " + c.AppUser.LastName, c.CreatedDate, c.AppUserId == owner)).ToListAsync(ct);
        var ids = rootDtos.Select(c => c.Id).ToArray();
        var children = await db.Comments.AsNoTracking().Where(c => c.BlogId == r.BlogId && c.IsApproved && c.ParentCommentId.HasValue && ids.Contains(c.ParentCommentId.Value))
            .OrderBy(c => c.CreatedDate).ThenBy(c => c.Id).Select(c => new CommentDto(c.Id, c.ParentCommentId, c.Content, c.AppUser.FirstName + " " + c.AppUser.LastName, c.CreatedDate, c.AppUserId == owner)).ToListAsync(ct);
        var grouped = children.ToLookup(c => c.ParentCommentId);
        return new(rootDtos.Select(root => new CommentThreadDto(root, grouped[root.Id].ToArray())).ToArray(), count, r.PageIndex, r.PageSize);
    }
    public async Task<PaginatedList<PendingCommentDto>> Handle(GetPendingCommentsQuery r, CancellationToken ct)
    {
        if (!(await auth.AuthorizeAsync(user.User, AccessPolicies.CommentModerator)).Succeeded) throw new UnauthorizedAccessException();
        var query = db.Comments.AsNoTracking().Where(c => !c.IsApproved)
            .OrderBy(c => c.CreatedDate).ThenBy(c => c.Id).Select(c => new PendingCommentDto(c.Id, c.BlogId, c.ParentCommentId, c.Content, c.AppUser.FirstName + " " + c.AppUser.LastName, c.CreatedDate));
        return await BlogProjection.Page(query, r.PageIndex, r.PageSize, ct);
    }
    public async Task<List<OwnCommentDto>> Handle(GetMyCommentsQuery r, CancellationToken ct)
    {
        if (!(await auth.AuthorizeAsync(user.User, AccessPolicies.Member)).Succeeded || !int.TryParse(user.UserId, out var id)) throw new UnauthorizedAccessException();
        return await db.Comments.AsNoTracking().Where(c => c.BlogId == r.BlogId && c.AppUserId == id).OrderByDescending(c => c.CreatedDate).ThenByDescending(c => c.Id).Take(20).Select(c => new OwnCommentDto(c.Id, c.Content, c.IsApproved)).ToListAsync(ct);
    }
}
