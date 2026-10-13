using BusinessLayer.Abstract;
using FluentValidation;
using Microsoft.Extensions.Localization;
using SkiaSharp;

namespace WebUI.Services;

public sealed class ImageOptimizationService(IWebHostEnvironment environment,
    IStringLocalizer<BusinessLayer.SharedResource> text) : IImageOptimizationService
{
    public const long MaximumFileBytes = 5 * 1024 * 1024;
    public const long MaximumPixels = 30_000_000;
    public const int MaximumWidth = 1200;

    public async Task<string?> StoreAsync(IFormFile? file, string folder, CancellationToken cancellationToken = default)
    {
        if (folder is not ("blogs" or "avatars")) throw new ArgumentException("Unsupported image storage folder.", nameof(folder));
        if (file is null) return null;
        if (file.Length is <= 0 or > MaximumFileBytes) throw new ValidationException(text["ImageTooLarge"]);

        await using var input = file.OpenReadStream();
        using var bytes = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (bytes.Length + read > MaximumFileBytes) throw new ValidationException(text["ImageTooLarge"]);
            await bytes.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (bytes.Length == 0) throw new ValidationException(text["ImageInvalidFormat"]);

        var signature = DetectSignature(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var mime = file.ContentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (signature is null || !MatchesMetadata(signature, extension, mime))
            throw new ValidationException(text["ImageInvalidFormat"]);

        using var encodedInput = SKData.CreateCopy(bytes.GetBuffer().AsSpan(0, (int)bytes.Length));
        using var codec = SKCodec.Create(encodedInput);
        if (codec is null || !string.Equals(codec.EncodedFormat.ToString(), signature, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(text["ImageInvalidFormat"]);
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 12000 || info.Height > 12000 || (long)info.Width * info.Height > MaximumPixels)
            throw new ValidationException(text["ImageTooManyPixels"]);
        // Decode only one frame. Reject large animated sequences before allocating pixel buffers.
        if (codec.FrameCount > 30) throw new ValidationException(text["ImageTooManyPixels"]);
        using var decoded = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
            throw new ValidationException(text["ImageInvalidFormat"]);
        cancellationToken.ThrowIfCancellationRequested();

        var rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = rotated ? info.Height : info.Width;
        var height = rotated ? info.Width : info.Height;
        var targetWidth = Math.Min(width, MaximumWidth);
        var targetHeight = Math.Max(1, (int)Math.Round((double)height * targetWidth / width));
        using var optimized = new SKBitmap(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(optimized))
        using (var original = SKImage.FromBitmap(decoded))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)targetWidth / width, (float)targetHeight / height);
            canvas.Concat(OrientationMatrix(codec.EncodedOrigin, info.Width, info.Height));
            canvas.DrawImage(original, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        }
        // Pixel re-encoding strips GPS, EXIF, comments and other uploaded metadata.
        using var resultImage = SKImage.FromBitmap(optimized);
        using var webp = resultImage.Encode(SKEncodedImageFormat.Webp, 75);
        if (webp is null) throw new ValidationException(text["ImageInvalidFormat"]);

        var uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        var targetFolder = Path.GetFullPath(Path.Combine(uploadsRoot, folder));
        if (!targetFolder.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Image path escapes uploads storage.");
        Directory.CreateDirectory(targetFolder);
        var name = $"{Guid.NewGuid():N}.webp";
        var path = Path.Combine(targetFolder, name);
        // Keep old files so soft-deleted content can be restored with its image.
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await output.WriteAsync(webp.ToArray(), cancellationToken);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
        return name;
    }

    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity
    };

    private static bool MatchesMetadata(string format, string extension, string? mime) => format switch
    {
        "JPEG" => extension is ".jpg" or ".jpeg" && mime == "image/jpeg",
        "PNG" => extension == ".png" && mime == "image/png",
        "GIF" => extension == ".gif" && mime == "image/gif",
        "WEBP" => extension == ".webp" && mime == "image/webp",
        _ => false
    };

    private static string? DetectSignature(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff) return "JPEG";
        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "PNG";
        if (header.Length >= 6 && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8))) return "GIF";
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8)) return "WEBP";
        return null;
    }
}
