using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class DeliverableBreakdown
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project Project { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DeliverablePriority Priority { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedHours { get; set; } = 0m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<DeliverableTask> Tasks { get; set; } = new List<DeliverableTask>();
    }
}

