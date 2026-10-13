using BusinessLayer.Abstract;
namespace WebUI.Services;
public sealed class BlogImageStorage(IImageOptimizationService images) : IBlogImageStorage
{
    public async Task<string?> StoreAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        var filename = await images.StoreAsync(file, "blogs", cancellationToken);
        return filename is null ? null : $"uploads/blogs/{filename}";
    }
}
