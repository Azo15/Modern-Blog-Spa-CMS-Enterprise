namespace WebUI.Helpers;

public static class ServiceCategories
{
    public static string For(string? title)
    {
        var slug = GlobalHelper.ToSlug(title ?? "");
        if (slug.Contains("hamam") || slug.Contains("kese")) return "hammam";
        if (slug.Contains("sauna") || slug.Contains("buhar") || slug.Contains("detoks")) return "detox";
        if (slug.Contains("cilt") || slug.Contains("maske") || slug.Contains("peeling") || slug.Contains("keratin") || slug.Contains("sac")) return "care";
        return "massage";
    }
}
