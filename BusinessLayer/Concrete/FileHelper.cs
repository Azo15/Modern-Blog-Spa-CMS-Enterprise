using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Concrete;

/// <summary>Compatibility adapter for legacy image screens; all uploads use the same validated WebP pipeline.</summary>
public sealed class FileHelper(IWebHostEnvironment environment, IImageOptimizationService images) : IFileHelper
{
    private readonly string webRoot = Path.GetFullPath(environment.WebRootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static readonly StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public async Task<string?> UploadAsync(IFormFile file, string root)
    {
        var requestedRoot = Path.GetFullPath(root);
        RequireContainedPath(requestedRoot, allowRoot: true);
        RequireContainedPath(Path.Combine(webRoot, "uploads", "blogs"), allowRoot: false);
        var filename = await images.StoreAsync(file, "blogs");
        if (filename is null) return null;
        // A filename-only contract prevents a replacement storage implementation escaping the upload directory.
        if (filename != Path.GetFileName(filename) || filename.Contains('\\') || filename.Contains('/') || !filename.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Image storage returned an invalid filename.");
        return "uploads/blogs/" + filename;
    }

    public void Delete(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var relative = relativePath.TrimStart('/', '\\');
        if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.Split(['/', '\\']).Any(segment => segment is "." or ".."))
            throw new UnauthorizedAccessException("Image path escapes storage.");
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
        RequireContainedPath(fullPath, allowRoot: false);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public Task<string?> UpdateAsync(IFormFile file, string root, string? existingFilePath)
    {
        // A failed upload must leave the prior image intact; historical/soft-deleted records may still reference it.
        return UploadAsync(file, root);
    }

    private void RequireContainedPath(string path, bool allowRoot)
    {
        path = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!(allowRoot && path.Equals(webRoot, Comparison)) && !path.StartsWith(webRoot + Path.DirectorySeparatorChar, Comparison))
            throw new UnauthorizedAccessException("Image path escapes storage.");
        // Check each existing component; canonical lexical checks alone do not protect against symlinks/junctions.
        var current = webRoot;
        RejectReparsePoint(current);
        foreach (var segment in Path.GetRelativePath(webRoot, path).Split(Path.DirectorySeparatorChar))
        {
            if (segment == ".") continue;
            current = Path.Combine(current, segment);
            RejectReparsePoint(current);
        }
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("Linked image paths are not allowed.");
    }
}
