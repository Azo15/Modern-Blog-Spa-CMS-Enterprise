namespace WebUI.Services;

/// <summary>Preserves valid uploaded media and resolves missing legacy images to local editorial assets.</summary>
public sealed class PublicMediaService(IWebHostEnvironment environment)
{
    public string Image(string? storedPath, string fallback = "img/hero-1.webp")
    {
        var candidate = storedPath?.Trim().TrimStart('~', '/').Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(candidate) && !candidate.Contains(':') && !candidate.Contains('?') &&
            !candidate.Contains('#') && candidate.Split('/').All(p => p is not (".." or ".") && p.Length > 0))
        {
            var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var path = Path.Combine(root, candidate.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return "/" + candidate;
        }
        return "/" + fallback.TrimStart('/');
    }
    public string ServiceImage(string? title, string? path)
    {
        var text = (title ?? "").ToLowerInvariant();
        var name = text.Contains("hamam") ? "hamam" : text.Contains("sauna") || text.Contains("buhar") ? "sauna" :
            text.Contains("derin") ? "derindoku" : text.Contains("refleks") ? "refleksoloji" :
            text.Contains("maske") || text.Contains("cilt") || text.Contains("peeling") ? "altinmaske" : "isvec";
        return Image(path, $"img/service-{name}.webp");
    }
    public string BlogImage(string? title, string? path)
    {
        var text = (title ?? "").ToLowerInvariant();
        var name = text.Contains("lavanta") || text.Contains("yağ") || text.Contains("difüz") ? "lavanta" :
            text.Contains("masaj") || text.Contains("doku") || text.Contains("refleks") ? "derindoku" :
            text.Contains("vitamin") || text.Contains("kolajen") ? "vitamin" :
            text.Contains("sauna") || text.Contains("hamam") ? "sauna" :
            text.Contains("yoga") || text.Contains("pilates") || text.Contains("egzersiz") ? "yoga" :
            text.Contains("nefes") || text.Contains("mindfulness") || text.Contains("medit") || text.Contains("stres") ? "nefes" :
            text.Contains("su ") || text.Contains("su iç") ? "su" : text.Contains("detoks") ? "detoks" : "cilt";
        return Image(path, $"img/blog-{name}.webp");
    }
    public string GalleryImage(string? title, string? path) => Image(path,
        (title ?? "").ToLowerInvariant() switch
        {
            var x when x.Contains("sauna") => "img/gallery-2.webp",
            var x when x.Contains("hamam") => "img/service-hamam.webp",
            var x when x.Contains("havuz") => "img/gallery-8.webp",
            var x when x.Contains("çift") => "img/gallery-7.webp",
            var x when x.Contains("salon") || x.Contains("dinlen") => "img/gallery-4.webp",
            var x when x.Contains("ürün") || x.Contains("aroma") => "img/blog-lavanta.webp",
            var x when x.Contains("yoga") || x.Contains("bahçe") => "img/blog-yoga.webp",
            _ => "img/gallery-1.webp"
        });
    public string Avatar(string? path) => Image(path, "img/portrait-author.webp");
}
