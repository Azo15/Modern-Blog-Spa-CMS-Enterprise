using Microsoft.AspNetCore.DataProtection;
namespace WebUI.Infrastructure;

public static class PersistentProtectionKeys
{
    public static string? ResolvePath(string contentRoot, string environment, string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath) && environment is "Development" or "Testing") return null;
        var root = Path.GetFullPath(contentRoot);
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(Path.Combine(root, "..", ".xspa-data-protection"))
            : Path.GetFullPath(configuredPath, root);
        var relative = Path.GetRelativePath(root, path);
        if (relative == "." || (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            throw new InvalidOperationException("DataProtection:KeysPath yayın klasörünün dışında özel bir dizin olmalıdır.");
        return path;
    }

    public static IDataProtectionBuilder AddPersistentProtection(this IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        var protection = services.AddDataProtection().SetApplicationName("ModernBlogPlatform");
        var path = ResolvePath(environment.ContentRootPath, environment.EnvironmentName, configuration["DataProtection:KeysPath"]);
        if (path is not null)
        {
            // The sibling directory survives replacement of the published application.
            // In shared hosting it inherits the site's private parent-directory permissions.
            if (OperatingSystem.IsWindows()) Directory.CreateDirectory(path);
            else Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            protection.PersistKeysToFileSystem(new DirectoryInfo(path));
        }
        return protection;
    }
}
