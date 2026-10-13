using BusinessLayer.Abstract;
using Ganss.Xss;

namespace BusinessLayer.Concrete;

public sealed class HtmlContentSanitizer : IHtmlContentSanitizer
{
    private readonly HtmlSanitizer sanitizer = new();
    public string Sanitize(string? html) => sanitizer.Sanitize(html ?? string.Empty);
}
