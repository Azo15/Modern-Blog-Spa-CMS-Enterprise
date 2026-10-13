using System.Globalization;
using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace BusinessLayer.Concrete;

public sealed class ReportService(AppDbContext db, ICurrentUserService currentUser, IAuthorizationService authorization,
    IStringLocalizer<SharedResource> text) : IReportService
{
    public const int MaximumRows = 10000;

    public async Task<ReportFile> GenerateAsync(ReportKind kind, ReportFormat format, ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var policy = kind == ReportKind.Contacts ? AccessPolicies.Moderator : AccessPolicies.Admin;
        if (!(await authorization.AuthorizeAsync(currentUser.User, policy)).Succeeded) throw new UnauthorizedAccessException();
        Validate(kind, format, filter);
        var culture = CultureInfo.CurrentUICulture;
        var english = culture.TwoLetterISOLanguageName == "en";
        string Label(string tr, string en) => english ? en : tr;
        List<object?[]> rows;
        string title;
        string[] columns;

        switch (kind)
        {
            case ReportKind.Contacts:
            {
                title = Label("İletişim Mesajları", "Contact Messages");
                columns = ["ID", Label("Ad Soyad", "Full Name"), Label("E-posta", "Email"), Label("Konu", "Subject"), Label("Durum", "Status"), Label("Gönderim (UTC)", "Sent (UTC)")];
                var query = db.ContactForms.AsNoTracking();
                if (filter.FromUtc.HasValue) query = query.Where(c => c.CreatedDate >= filter.FromUtc.Value);
                if (filter.ToUtc.HasValue) query = query.Where(c => c.CreatedDate < filter.ToUtc.Value);
                var data = await query.OrderByDescending(c => c.CreatedDate).ThenBy(c => c.Id).Take(MaximumRows + 1)
                    .Select(c => new { c.Id, c.FullName, c.Email, c.Subject, c.Status, c.CreatedDate }).ToListAsync(cancellationToken);
                EnsureSize(data.Count);
                rows = data.Select(c => new object?[] { c.Id, c.FullName, c.Email, c.Subject, c.Status.ToString(), c.CreatedDate }).ToList();
                break;
            }
            case ReportKind.Blogs:
            {
                title = Label("Blog İstatistikleri", "Blog Statistics");
                columns = ["ID", Label("Başlık", "Title"), Label("Yazar ID", "Author ID"), Label("Yayın Durumu", "Publish Status"), Label("Favoriler", "Favorites"), Label("Tepkiler", "Reactions"), Label("Onaylı Yorumlar", "Approved Comments"), Label("Oluşturma (UTC)", "Created (UTC)")];
                var query = db.Blogs.AsNoTracking();
                if (filter.FromUtc.HasValue) query = query.Where(b => b.CreatedDate >= filter.FromUtc.Value);
                if (filter.ToUtc.HasValue) query = query.Where(b => b.CreatedDate < filter.ToUtc.Value);
                if (filter.UserId.HasValue) query = query.Where(b => b.AppUserId == filter.UserId.Value);
                if (filter.BlogId.HasValue) query = query.Where(b => b.Id == filter.BlogId.Value);
                var data = await query.OrderByDescending(b => b.CreatedDate).ThenBy(b => b.Id).Take(MaximumRows + 1)
                    .Select(b => new
                    {
                        b.Id, b.Title, b.AppUserId, b.PublishStatus, b.CreatedDate,
                        Favorites = db.Set<BlogFavorite>().Count(f => f.BlogId == b.Id),
                        Reactions = db.Set<BlogReaction>().Count(r => r.BlogId == b.Id),
                        Comments = db.Comments.Count(c => c.BlogId == b.Id && c.IsApproved)
                    }).ToListAsync(cancellationToken);
                EnsureSize(data.Count);
                rows = data.Select(b => new object?[] { b.Id, b.Title, b.AppUserId, StatusLabel(b.PublishStatus, english), b.Favorites, b.Reactions, b.Comments, b.CreatedDate }).ToList();
                break;
            }
            case ReportKind.Interactions:
            {
                title = Label("Kullanıcı Etkileşimleri", "User Interactions");
                columns = [Label("Üye ID", "User ID"), Label("Ad Soyad", "Full Name"), "Blog ID", Label("Başlık", "Title"), Label("Etkileşim", "Interaction"), Label("Oluşturma (UTC)", "Created (UTC)")];
                // Join active blog records explicitly: interactions on trashed content stay out of exports.
                var favorites = from f in db.Set<BlogFavorite>().AsNoTracking()
                                join b in db.Blogs.AsNoTracking() on f.BlogId equals b.Id
                                join u in db.Users.AsNoTracking() on f.AppUserId equals u.Id
                                select new ActivityRow(u.Id, u.FirstName + " " + u.LastName, b.Id, b.Title, 0, f.CreatedDate);
                var reactions = from r in db.Set<BlogReaction>().AsNoTracking()
                                join b in db.Blogs.AsNoTracking() on r.BlogId equals b.Id
                                join u in db.Users.AsNoTracking() on r.AppUserId equals u.Id
                                select new ActivityRow(u.Id, u.FirstName + " " + u.LastName, b.Id, b.Title, (int)r.ReactionType, r.CreatedDate);
                var query = favorites.Concat(reactions);
                if (filter.FromUtc.HasValue) query = query.Where(r => r.CreatedDate >= filter.FromUtc.Value);
                if (filter.ToUtc.HasValue) query = query.Where(r => r.CreatedDate < filter.ToUtc.Value);
                if (filter.UserId.HasValue) query = query.Where(r => r.UserId == filter.UserId.Value);
                if (filter.BlogId.HasValue) query = query.Where(r => r.BlogId == filter.BlogId.Value);
                var data = await query.OrderByDescending(r => r.CreatedDate).ThenBy(r => r.UserId).Take(MaximumRows + 1).ToListAsync(cancellationToken);
                EnsureSize(data.Count);
                rows = data.Select(r => new object?[] { r.UserId, r.UserName, r.BlogId, r.BlogTitle, ReactionLabel(r.Kind, english), r.CreatedDate }).ToList();
                break;
            }
            case ReportKind.Users:
            {
                title = Label("Kullanıcı İstatistikleri", "User Statistics");
                columns = ["ID", Label("Ad Soyad", "Full Name"), Label("E-posta", "Email"), Label("Aktif", "Active"), Label("Kilit Bitişi (UTC)", "Lockout End (UTC)"), Label("Favoriler", "Favorites"), Label("Tepkiler", "Reactions")];
                var query = db.Users.AsNoTracking();
                if (filter.UserId.HasValue) query = query.Where(u => u.Id == filter.UserId.Value);
                var data = await query.OrderBy(u => u.Id).Take(MaximumRows + 1).Select(u => new
                {
                    u.Id, u.FirstName, u.LastName, u.Email, u.IsActive, u.LockoutEnd,
                    Favorites = db.Set<BlogFavorite>().Count(f => f.AppUserId == u.Id && !f.Blog.IsDeleted),
                    Reactions = db.Set<BlogReaction>().Count(r => r.AppUserId == u.Id && !r.Blog.IsDeleted)
                }).ToListAsync(cancellationToken);
                EnsureSize(data.Count);
                rows = data.Select(u => new object?[] { u.Id, u.FirstName + " " + u.LastName, u.Email, u.IsActive ? Label("Evet", "Yes") : Label("Hayır", "No"), u.LockoutEnd?.UtcDateTime, u.Favorites, u.Reactions }).ToList();
                break;
            }
            default: throw new ValidationException(text["InvalidReportFilter"]);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (rows.SelectMany(row => row).OfType<string>().Any(value => value.Length > 32767)) throw new ValidationException(text["ReportTooLarge"]);
        var document = new ReportDocument(title, columns, rows, FilterDescription(filter, english), culture);
        var content = format == ReportFormat.Excel ? ReportDocumentWriter.BuildExcel(document) : ReportDocumentWriter.BuildPdf(document);
        var extension = format == ReportFormat.Excel ? "xlsx" : "pdf";
        var contentType = format == ReportFormat.Excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf";
        return new ReportFile(content, contentType, $"{kind.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}", rows.Count);
    }

    private void Validate(ReportKind kind, ReportFormat format, ReportFilter filter)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(format) || filter.UserId <= 0 || filter.BlogId <= 0
            || filter.FromUtc.HasValue && filter.ToUtc.HasValue && filter.FromUtc >= filter.ToUtc
            || filter.FromUtc?.Kind == DateTimeKind.Local || filter.ToUtc?.Kind == DateTimeKind.Local
            || kind == ReportKind.Contacts && (filter.UserId.HasValue || filter.BlogId.HasValue)
            || kind == ReportKind.Users && (filter.BlogId.HasValue || filter.FromUtc.HasValue || filter.ToUtc.HasValue))
            throw new ValidationException(text["InvalidReportFilter"]);
    }

    private void EnsureSize(int count) { if (count > MaximumRows) throw new ValidationException(text["ReportTooLarge"]); }
    private static string StatusLabel(PublishStatus status, bool english) => english ? status.ToString() : status switch
    { PublishStatus.Draft => "Taslak", PublishStatus.PendingReview => "İnceleme Bekliyor", PublishStatus.Published => "Yayında", PublishStatus.Rejected => "Reddedildi", _ => status.ToString() };
    private static string ReactionLabel(int kind, bool english) => kind switch
    { 0 => english ? "Favorite" : "Favori", 1 => english ? "Like" : "Beğeni", 2 => english ? "Heart" : "Kalp", 3 => english ? "Clap" : "Alkış", _ => "?" };
    private static string FilterDescription(ReportFilter filter, bool english)
    {
        var from = filter.FromUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "*";
        var to = filter.ToUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "*";
        return english
            ? $"UTC dates: {from} (inclusive) / {to} (exclusive). User: {filter.UserId?.ToString() ?? "*"}; Blog: {filter.BlogId?.ToString() ?? "*"}."
            : $"UTC tarihleri: {from} (dahil) / {to} (hariç). Üye: {filter.UserId?.ToString() ?? "*"}; Blog: {filter.BlogId?.ToString() ?? "*"}.";
    }
    private sealed record ActivityRow(int UserId, string UserName, int BlogId, string? BlogTitle, int Kind, DateTime CreatedDate);
}
