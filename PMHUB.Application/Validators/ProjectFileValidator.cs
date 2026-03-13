using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.Validators
{
    public static class ProjectFileValidator
    {
        private static readonly string[] AllowedExtensions =
            { ".pdf", ".xlsx", ".docx", ".pptx" };

        private const long MaxFileSize = 10 * 1024 * 1024;  

        public static void Validate(UploadProjectFileDto dto)
        {
             if (dto.File == null || dto.File.Length == 0)
                throw new BadRequestException("Le fichier est vide ou manquant.");

             var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new BadRequestException(
                    $"Extension '{extension}' non autorisée. Extensions acceptées : {string.Join(", ", AllowedExtensions)}");

             if (dto.File.Length > MaxFileSize)
                throw new BadRequestException(
                    $"Fichier trop volumineux. Taille maximale : {MaxFileSize / 1024 / 1024} MB");

             var allowedContentTypes = new[]
            {
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            };

            if (!allowedContentTypes.Contains(dto.File.ContentType))
                throw new BadRequestException(
                    $"Type de contenu '{dto.File.ContentType}' non autorisé.");
        }
    }
}
