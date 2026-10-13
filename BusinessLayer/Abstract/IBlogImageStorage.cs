using Microsoft.AspNetCore.Http;
namespace BusinessLayer.Abstract;
public interface IBlogImageStorage
{
    Task<string?> StoreAsync(IFormFile? file, CancellationToken cancellationToken);
}
