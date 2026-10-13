using System.Text.Json;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;

/// <summary>Archive old unreferenced trash without destroying audit history or restorable image files.</summary>
public sealed class TrashMaintenanceJob(AppDbContext db, IHostEnvironment environment,
    ILogger<TrashMaintenanceJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var archived = db.Set<TrashArchiveEntry>();
        var blogs = await db.Blogs.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.IsDeleted && b.DeletedDate < cutoff &&
                !db.Comments.IgnoreQueryFilters().Any(c => c.BlogId == b.Id) &&
                !db.Set<BlogFavorite>().IgnoreQueryFilters().Any(f => f.BlogId == b.Id) &&
                !db.Set<BlogReaction>().IgnoreQueryFilters().Any(r => r.BlogId == b.Id) &&
                !archived.Any(a => a.EntityType == nameof(Blog) && a.EntityId == b.Id))
            .OrderBy(b => b.Id).Take(200).ToListAsync(cancellationToken);
        foreach (var blog in blogs)
            archived.Add(Entry(nameof(Blog), blog.Id, new
            {
                blog.Title, blog.SubTitle, blog.Content, blog.Image, blog.Slug, blog.PublishStatus,
                blog.AppUserId, blog.CatagoryId, blog.CreatedDate, blog.CreatedBy, blog.UpdatedDate,
                blog.UpdatedBy, blog.DeletedDate, blog.DeletedBy
            }));

        var categories = await db.Catagories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.IsDeleted && c.DeletedDate < cutoff &&
                !db.Blogs.IgnoreQueryFilters().Any(b => b.CatagoryId == c.Id) &&
                !archived.Any(a => a.EntityType == nameof(Catagory) && a.EntityId == c.Id))
            .OrderBy(c => c.Id).Take(200).ToListAsync(cancellationToken);
        foreach (var category in categories)
            archived.Add(Entry(nameof(Catagory), category.Id, new
            {
                category.Title, category.CreatedDate, category.CreatedBy, category.UpdatedDate,
                category.UpdatedBy, category.DeletedDate, category.DeletedBy
            }));

        var galleries = await db.Galaries.IgnoreQueryFilters().AsNoTracking()
            .Where(g => g.IsDeleted && g.DeletedDate < cutoff &&
                !archived.Any(a => a.EntityType == nameof(Galary) && a.EntityId == g.Id))
            .OrderBy(g => g.Id).Take(200).ToListAsync(cancellationToken);
        foreach (var gallery in galleries)
            archived.Add(Entry(nameof(Galary), gallery.Id, new
            {
                gallery.Title, gallery.Image, gallery.CreatedDate, gallery.CreatedBy,
                gallery.UpdatedDate, gallery.UpdatedBy, gallery.DeletedDate, gallery.DeletedBy
            }));
        await db.SaveChangesAsync(cancellationToken);
        var logs = ArchiveOldLogFiles(cutoff);
        logger.LogInformation("Maintenance archived {RecordCount} trash records and {LogCount} old log files; originals/images retained.",
            blogs.Count + categories.Count + galleries.Count, logs);
    }

    private static TrashArchiveEntry Entry(string type, int id, object snapshot)
        => new() { EntityType = type, EntityId = id, SnapshotJson = JsonSerializer.Serialize(snapshot) };

    private int ArchiveOldLogFiles(DateTime cutoff)
    {
        // Only the application's named daily logs; never enumerate wwwroot or upload/image directories.
        var root = Path.GetFullPath(environment.ContentRootPath);
        var logs = Path.GetFullPath(Path.Combine(root, "logs"));
        var archive = Path.GetFullPath(Path.Combine(logs, "archive"));
        if (!logs.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(logs) || HasReparsePoint(logs)) return 0;
        Directory.CreateDirectory(archive);
        if (HasReparsePoint(archive)) return 0;
        var count = 0;
        foreach (var path in Directory.EnumerateFiles(logs, "log-*.txt", SearchOption.TopDirectoryOnly))
        {
            var file = new FileInfo(path);
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0 || file.LastWriteTimeUtc >= cutoff)
                continue;
            // Serilog keeps its active log open; requiring exclusive access excludes it.
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                var destination = Path.Combine(archive, file.Name);
                if (File.Exists(destination)) continue;
                File.Move(path, destination);
                count++;
            }
            catch (IOException) { /* Active or concurrently archived log; leave it for another run. */ }
            catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Log archive access denied."); }
        }
        return count;
    }

    private static bool HasReparsePoint(string path)
        => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}
