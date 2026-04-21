using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class ProjectRoadblock
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public RoadblockStatus Status { get; set; } = RoadblockStatus.Open;

        [Required]
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;

        // Date cible attendue (utilisée pour “delayed”)
        [Required]
        public DateTime DueAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

