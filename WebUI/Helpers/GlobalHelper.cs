// Dd.cs  (Laravel'deki dd() gibi)
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebUI.Helpers;

public static class GlobalHelper
{
    public static string ToSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        text = text.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Türkçe karakterleri dönüştür
        text = text
            .Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u")
            .Replace("ö", "o").Replace("ı", "i").Replace("ç", "c");

        // Harf ve rakam dışındakileri tire ile değiştir
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "-");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"-+", "-");

        return text.Trim('-');
    }
}

