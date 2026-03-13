using System;

namespace PMHUB.Domain.Entities
{
    public class InternAllocation
    {
        public Guid Id { get; set; }

         public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

         public Guid InternId { get; set; }
        public Intern Intern { get; set; } = null!;

         public decimal AllocatedHours { get; set; } = 0m;
        public decimal HoursWorked { get; set; } = 0m;

         public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

         public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();

    }
}