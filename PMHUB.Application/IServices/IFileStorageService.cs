using Microsoft.AspNetCore.Http;

namespace PMHUB.Application.IServices
{
    public interface IFileStorageService
    {
         Task<string> SaveFileAsync(IFormFile file, Guid projectId, string folder);

         Task DeleteFileAsync(string filePath);

         Task<byte[]> GetFileAsync(string filePath);

         string GetFileUrl(string filePath);
    }
}