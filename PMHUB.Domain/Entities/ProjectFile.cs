using PMHUB.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMHUB.Domain.Entities
{
    public class ProjectFile
    {
        public Guid Id { get; set; }

        // Relation avec le projet
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Informations sur le fichier
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;  
        public ProjectFileType FileType { get; set; } = ProjectFileType.Other;
        public string? Description { get; set; }

        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}