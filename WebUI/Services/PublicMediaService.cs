using System.Security.Cryptography;
using System.Text;
using WebUI.Helpers;

namespace WebUI.Services;

/// <summary>Independent editorial photos for demo entities; real custom uploads are preserved.</summary>
public sealed class PublicMediaService(IWebHostEnvironment environment)
{
    private static readonly IReadOnlyDictionary<string, string> Services = Map(
        ("Klasik İsveç Masajı", "service-isvec"), ("Türk Hamamı Ritüeli", "service-hamam"),
        ("Altın Yüz Maskesi", "service-altinmaske"), ("Derin Doku Masajı", "service-derindoku"),
        ("Aromaterapi Masajı", "service-aromaterapi"), ("Çiftler Masajı", "service-ciftler"),
        ("Refleksoloji", "service-refleksoloji"), ("Sauna & Buhar Odası", "service-sauna"),
        ("Keratin Saç Bakımı", "service-keratin"), ("Cilt Peeling Seansı", "service-peeling"),
        ("Bebek Masajı Kursu", "service-bebek"), ("Hamilelik Masajı", "service-hamilelik"));

    private static readonly IReadOnlyDictionary<string, string> Blogs = Map(
        ("Kış Aylarında Cilt Neminin Önemi", "blog-cilt"),
        ("C Vitamini Serumu Neden Kullanmalısınız?", "blog-vitamin"),
        ("Peeling Nedir ve Ne Zaman Yapılmalı?", "blog-peeling"),
        ("İsveç Masajı ve Faydaları", "blog-isvec"),
        ("Derin Doku Masajı Kimler İçin Uygundur?", "blog-derindoku"),
        ("Refleksoloji: Ayak Noktalarından Şifa", "blog-refleksoloji"),
        ("5 Dakikalık Nefes Meditasyonu", "blog-nefes"),
        ("Mindfulness ve Günlük Hayat", "blog-mindfulness"),
        ("Dijital Detoks: Telefondan Uzak Kalmak", "blog-dijital-detoks"),
        ("Lavanta Yağının 7 Faydası", "blog-lavanta"), ("Difüzör Kullanımı Rehberi", "blog-difuzor"),
        ("3 Günlük Bahar Detoks Programı", "blog-detoks"),
        ("Kolajen Üretimini Artıran 8 Besin", "blog-kolajen"),
        ("Türk Hamamı Deneyimi: Neler Beklemelisiniz?", "blog-hamam"),
        ("Sauna ve Bağışıklık Sistemi İlişkisi", "blog-sauna"),
        ("Retinol Kullanımının Püf Noktaları", "blog-retinol"),
        ("Gözaltı Kırışıklıklarına Karşı Doğal Yöntemler", "blog-gozalti"),
        ("Keratin Tedavisi Nedir ve Ne Kadar Sürer?", "blog-keratin"),
        ("Hindistan Cevizi Yağının Saça Faydaları", "blog-hindistancevizi"),
        ("Sabah Yogası: 10 Dakikada Güne Başlangıç", "blog-yoga"),
        ("Pilates ve Duruş Bozukluğu", "blog-pilates"),
        ("Düzenli Su İçmenin Cilde Etkisi", "blog-su"),
        ("Uyku Kalitesi Cilt Sağlığını Nasıl Etkiler?", "blog-uyku"),
        ("Egzersiz Sonrası Cilt Bakımı Rutini", "blog-bakim"),
        ("Stres ve Cilt: Kortizolün Etkisi", "blog-stres"),
        ("Güneş Koruyucu Her Mevsim Şart mı?", "blog-gunes"),
        ("Spa Öncesi Yapılmaması Gerekenler", "blog-spa-oncesi"),
        ("Hamilelikte Güvenli Spa Hizmetleri", "blog-hamilelik"),
        ("İlk Kez Spa'ya Gidenler İçin Rehber", "blog-ilk-spa"));

    private static readonly IReadOnlyDictionary<string, string> Galleries = Map(
        ("VIP Masaj Odası", "gallery-1"), ("Sauna Bölümü", "gallery-2"),
        ("Hamam Alanı", "gallery-3"), ("Resepsiyon", "gallery-4"),
        ("Aromaterapi Koleksiyonu", "gallery-5"), ("Dinlenme Salonu", "gallery-6"),
        ("Çift Masaj Odası", "gallery-7"), ("Havuz & Jakuzi", "gallery-8"),
        ("Makyaj & Saç Bakım Stüdyosu", "gallery-9"), ("Dış Terapi Bahçesi", "gallery-10"),
        ("Premium Ürün Vitrini", "gallery-11"), ("Yoga & Meditasyon Odası", "gallery-12"));

    private static readonly HashSet<string> EditorialPaths = Services.Values.Concat(Blogs.Values)
        .Concat(Galleries.Values).Concat(Enumerable.Range(1, 6).Select(i => $"vip-{i}"))
        .Concat(["hero-1", "hero-2", "hero-3", "about-retreat", "portrait-author", "portrait-therapist"])
        .Select(name => $"img/{name}.webp").ToHashSet(StringComparer.OrdinalIgnoreCase);

    public string Image(string? storedPath, string fallback = "img/hero-1.webp") =>
        ExistingPath(storedPath) is { } path ? "/" + path : "/" + fallback.TrimStart('/');

    public string ServiceImage(string? title, string? path, int entityId = 0) =>
        EntityImage("service", title, path, entityId, Services);

    public string BlogImage(string? title, string? path, int entityId = 0) =>
        EntityImage("blog", title, path, entityId, Blogs);

    public string GalleryImage(string? title, string? path, int entityId = 0) =>
        EntityImage("gallery", title, path, entityId, Galleries);

    public string Avatar(string? path) => Image(path, "img/portrait-author.webp");

    public static string Placeholder(string kind, int entityId, string? title)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{kind}:{entityId}:{title}")))
            .ToLowerInvariant()[..24];
        return $"/media/placeholder/{kind}/{key}.svg";
    }

    private string EntityImage(string kind, string? title, string? storedPath, int id,
        IReadOnlyDictionary<string, string> editorial)
    {
        var placeholder = Placeholder(kind, id, title);
        if (string.Equals(storedPath?.TrimStart('~', '/'), placeholder.TrimStart('/'), StringComparison.Ordinal))
            return placeholder;
        var existing = ExistingPath(storedPath);
        // Repair even valid seeded img paths, which were formerly shared by many entities.
        // A valid custom file (including everything under uploads/) always takes priority.
        if (existing is not null && !EditorialPaths.Contains(existing)) return "/" + existing;
        var candidate = storedPath?.Trim().TrimStart('~', '/').Replace('\\', '/');
        var legacy = candidate is not null && (EditorialPaths.Contains(candidate) ||
            candidate.StartsWith(kind + "/", StringComparison.OrdinalIgnoreCase) ||
            (kind == "gallery" && candidate.StartsWith("galery/", StringComparison.OrdinalIgnoreCase)));
        if ((id == 0 || legacy) && editorial.TryGetValue(GlobalHelper.ToSlug(title ?? ""), out var asset))
            return $"/img/{asset}.webp";
        return placeholder;
    }

    private string? ExistingPath(string? storedPath)
    {
        var candidate = storedPath?.Trim().TrimStart('~', '/').Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Contains(':') || candidate.Contains('?') ||
            candidate.Contains('#') || !candidate.Split('/').All(p => p is not (".." or ".") && p.Length > 0)) return null;
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath ?? AppContext.BaseDirectory, "wwwroot");
        return File.Exists(Path.Combine(root, candidate.Replace('/', Path.DirectorySeparatorChar))) ? candidate : null;
    }

    private static IReadOnlyDictionary<string, string> Map(params (string Title, string Asset)[] entries) =>
        entries.ToDictionary(x => GlobalHelper.ToSlug(x.Title), x => x.Asset, StringComparer.OrdinalIgnoreCase);
}
