using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.IO;
using System;
namespace BusinessLayer.Concrete
{
    public class FileHelper : IFileHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _wwwRootPath;

        public FileHelper(IWebHostEnvironment env)
        {
            _env = env;
            _wwwRootPath = _env.WebRootPath;
        }

        public async Task<string?> UploadAsync(IFormFile file, string root)
        {
            if (file == null || file.Length == 0)
                return null;

            EnsureDirectoryExists(root);

            string fileName = GenerateUniqueFileName(file.FileName);
            string fullPath = Path.Combine(root, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return GetRelativePath(root, fileName);
        }

        public void Delete(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return;

            var fullPath = Path.Combine(_wwwRootPath, relativePath.TrimStart('/', '\\'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public async Task<string?> UpdateAsync(IFormFile newFile, string root, string? oldRelativePath)
        {
            if (!string.IsNullOrEmpty(oldRelativePath))
            {
                Delete(oldRelativePath);
            }

            return await UploadAsync(newFile, root);
        }

        // Yardımcı metotlar

        private void EnsureDirectoryExists(string root)
        {
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName);
            return $"{Guid.NewGuid()}{extension}";
        }

        private string GetRelativePath(string root, string fileName)
        {
            string relativeRoot = Path.GetRelativePath(_wwwRootPath, root);
            return Path.Combine(relativeRoot, fileName).Replace("\\", "/");
        }
    }
}
