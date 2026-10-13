namespace BusinessLayer.Abstract;

public interface IHtmlContentSanitizer
{
    string Sanitize(string? html);
}
