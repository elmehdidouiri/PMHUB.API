using System;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Domain.Entities
{
    public class InternAllocation
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid InternId { get; set; }
        public Intern Intern { get; set; } = null!;

        public decimal AllocatedHours { get; set; } = 0m;
        public decimal HoursWorked { get; set; } = 0m;

        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();
        public ICollection<InternHourEntry> InternHourEntries { get; set; } = new List<InternHourEntry>();
    }
}
