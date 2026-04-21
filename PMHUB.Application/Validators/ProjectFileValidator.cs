using Microsoft.AspNetCore.Http;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;

namespace PMHUB.Application.Validators
{
    public static class ProjectFileValidator
    {
        private static readonly string[] AllowedExtensions =
            { ".pdf", ".xlsx", ".docx", ".pptx" };

        private const long MaxFileSize = 10 * 1024 * 1024;

        public static void Validate(UploadProjectFileDto dto)
        {
            Validate(dto.File);
        }

        public static void Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
            throw new BadRequestException("The file is missing or empty.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new BadRequestException(
                    $"Extension '{extension}' non autorisée. Extensions acceptées : {string.Join(", ", AllowedExtensions)}");

            if (file.Length > MaxFileSize)
                throw new BadRequestException(
                    $"Fichier trop volumineux. Taille maximale : {MaxFileSize / 1024 / 1024} MB");

            var allowedContentTypes = new[]
            {
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            };

            if (!allowedContentTypes.Contains(file.ContentType))
                throw new BadRequestException(
                    $"Type de contenu '{file.ContentType}' non autorisé.");
        }
    }
}
