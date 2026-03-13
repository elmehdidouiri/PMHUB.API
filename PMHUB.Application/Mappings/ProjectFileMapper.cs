using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.Mappings
{
    public static class ProjectFileMapper
    {
        public static ProjectFileDto ToDto(ProjectFile f, IFileStorageService storageService) => new()
        {
            Id = f.Id,
            ProjectId = f.ProjectId,
            FileType = f.FileType,
            OriginalFileName = f.OriginalFileName,
            FileUrl = storageService.GetFileUrl(f.FilePath),
            ContentType = f.ContentType,
            FileSize = f.FileSize,
            Description = f.Description,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        };

        public static IEnumerable<ProjectFileDto> ToDtoList(
            IEnumerable<ProjectFile> files,
            IFileStorageService storageService) =>
            files.Select(f => ToDto(f, storageService));
    }
}