using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BusinessLayer.Abstract;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Concrete;

public sealed class ProfanityFilterService(IConfiguration configuration) : IProfanityFilterService
{
    private readonly string[] words = configuration.GetSection("Moderation:BlockedWords").Get<string[]>()
        ?? ["aptal", "salak", "gerizekalı"];
    public bool IsAllowed(string content)
    {
        var normalized = content.Normalize(NormalizationForm.FormKC).ToLower(new CultureInfo("tr-TR"));
        return !words.Any(word => Regex.IsMatch(normalized, @"(?<!\p{L})" + Regex.Escape(word.ToLower(new CultureInfo("tr-TR"))) + @"(?!\p{L})"));
    }
}
