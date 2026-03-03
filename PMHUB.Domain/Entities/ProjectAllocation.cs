using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class ProjectAllocation
    {
        public Guid Id { get; set; }

        // Relation avec le projet
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Relation avec l’utilisateur
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Rôle et type
        public string Role { get; set; } = string.Empty;
        public AllocationType AllocationType { get; set; } = AllocationType.Resource;

        // Heures allouées
        public decimal AllocatedHours { get; set; } = 0m;
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}