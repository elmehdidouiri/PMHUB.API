using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class DeliverableTask
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DeliverableId { get; set; }

        [ForeignKey(nameof(DeliverableId))]
        public DeliverableBreakdown Deliverable { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DeliverableTaskCategory Category { get; set; } = DeliverableTaskCategory.Development;

        public int? StoryPoints { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DevHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UxHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TestingHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Hours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedHours { get; set; } = 0m;

        public TaskStatuss Status { get; set; } = TaskStatuss.NotStarted;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        // Une ressource au choix: soit un membre du projet, soit un intern déjà alloué.
        public Guid? ProjectMemberId { get; set; }
        [ForeignKey(nameof(ProjectMemberId))]
        public ProjectMember? ProjectMember { get; set; }

        public Guid? InternAllocationId { get; set; }
        [ForeignKey(nameof(InternAllocationId))]
        public InternAllocation? InternAllocation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

