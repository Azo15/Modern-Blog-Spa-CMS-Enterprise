using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BusinessLayer.Abstract
{
    public interface IFileHelper
    {
        Task<string?> UploadAsync(IFormFile file, string root);
        void Delete(string fullFilePath);
        Task<string> UpdateAsync(IFormFile file, string root, string? existingFilePath);
    }
}
