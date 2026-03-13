using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PMHUB.Application.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Infrastructure.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly string _baseUrl;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public LocalFileStorageService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _basePath = config["FileStorage:BasePath"]!;
            _maxFileSize = long.Parse(config["FileStorage:MaxFileSizeBytes"]!);
            _allowedExtensions = config.GetSection("FileStorage:AllowedExtensions")
                            .GetChildren()
                            .Select(c => c.Value!)
                            .ToArray();

            var request = httpContextAccessor.HttpContext?.Request;
            _baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : string.Empty;
        }

        public async Task<string> SaveFileAsync(IFormFile file, Guid projectId, string folder)
        {
             var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    $"Extension '{extension}' non autorisée. Extensions acceptées : {string.Join(", ", _allowedExtensions)}");

             if (file.Length > _maxFileSize)
                throw new InvalidOperationException(
                    $"Fichier trop volumineux. Taille maximale : {_maxFileSize / 1024 / 1024} MB");

             var directory = Path.Combine(
                Directory.GetCurrentDirectory(),
                _basePath,
                "projects",
                projectId.ToString(),
                folder);

            Directory.CreateDirectory(directory);

             var storedName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(directory, storedName);

             using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

             return Path.Combine("uploads", "projects", projectId.ToString(), folder, storedName)
                       .Replace("\\", "/");
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                _basePath.Replace("wwwroot/", "wwwroot\\"),
                filePath.Replace("uploads/", ""));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Fichier introuvable : {filePath}");

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                filePath);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        public string GetFileUrl(string filePath) =>
            $"{_baseUrl}/{filePath}";
    }
}
