using System;

namespace PMHUB.Domain.Entities
{
    public class InternAllocation
    {
        public Guid Id { get; set; }

        // Relation avec le projet
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Relation avec le stagiaire
        public Guid InternId { get; set; }
        public Intern Intern { get; set; } = null!;

        // Heures allouées et suivies
        public decimal AllocatedHours { get; set; } = 0m;
        public decimal HoursWorked { get; set; } = 0m;

        // Dates
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();

    }
}