using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Abstract;

/// <summary>Validates and writes a generated WebP filename into an approved uploads folder.</summary>
public interface IImageOptimizationService
{
    Task<string?> StoreAsync(IFormFile? file, string folder, CancellationToken cancellationToken = default);
}
