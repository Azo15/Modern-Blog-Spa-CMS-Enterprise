using BusinessLayer.Abstract;
using FluentValidation;
namespace WebUI.Services;
public sealed class BlogImageStorage(IFileHelper files, IWebHostEnvironment environment) : IBlogImageStorage
{
    public async Task<string?> StoreAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length is <= 0 or > 5242880 || !new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
            throw new ValidationException("Yalnızca 5 MB sınırındaki JPG, PNG, GIF veya WebP görselleri yüklenebilir.");
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header, cancellationToken);
        var valid = read >= 3 && extension is ".jpg" or ".jpeg" && header[0] == 255 && header[1] == 216 && header[2] == 255
            || read >= 8 && extension == ".png" && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || read >= 6 && extension == ".gif" && System.Text.Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a"
            || read >= 12 && extension == ".webp" && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        if (!valid) throw new ValidationException("Görselin dosya biçimi geçersiz.");
        return await files.UploadAsync(file, Path.Combine(environment.WebRootPath, "uploads", "blogs"));
    }
}
