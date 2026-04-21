using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class ProjectFileDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public ProjectFileType FileType { get; set; }
        public string FileTypeLabel => FileType.ToString();
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeLabel => FileSize switch
        {
            < 1024 => $"{FileSize} B",
            < 1024 * 1024 => $"{FileSize / 1024} KB",
            _ => $"{FileSize / 1024 / 1024} MB"
        };
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ProjectFileVersionDto> Versions { get; set; } = new List<ProjectFileVersionDto>();
    }
}
