using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class ProjectTimelineEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; } = null!;

        // La date à laquelle la phase a été atteinte.
        [Required]
        public DateTime Date { get; set; }

        // Phase (Pipeline/PreProcess/Initiation/...) + libellé custom (Phase 1/2/3...)
        [Required]
        public ProjectPhase Phase { get; set; }

        [Required]
        public int PhaseOrder { get; set; }

        [Required, MaxLength(200)]
        public string PhaseLabel { get; set; } = string.Empty;

        // Sponsor obligatoire pour chaque entrée du timeline.
        [Required]
        public Guid SponsorUserId { get; set; }

        [ForeignKey(nameof(SponsorUserId))]
        public NormalUser SponsorUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

