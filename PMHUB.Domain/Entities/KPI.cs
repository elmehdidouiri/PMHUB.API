using System;

namespace PMHUB.Domain.Entities
{
    public class KPI
    {
        public Guid Id { get; set; }

        // Relation avec le projet
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Informations KPI
        public string Name { get; set; } = string.Empty;  
        public decimal TargetValue { get; set; } = 0m;
        public decimal CurrentValue { get; set; } = 0m;
        public string? Description { get; set; }

        // Dates et suivi
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}