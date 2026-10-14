namespace WebUI.Services;

/// <summary>Rejects lexical traversal and existing links before application-controlled image writes.</summary>
internal static class ImageStoragePathGuard
{
    public static string ResolveFolder(string webRoot, string folder)
    {
        var root = Path.GetFullPath(webRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var target = Path.GetFullPath(Path.Combine(root, "uploads", folder));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, comparison))
            throw new UnauthorizedAccessException("Image path escapes storage.");
        var current = root;
        RequireUnlinked(current);
        foreach (var segment in Path.GetRelativePath(root, target).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            RequireUnlinked(current);
        }
        return target;
    }

    private static void RequireUnlinked(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("Linked image paths are not allowed.");
    }
}
