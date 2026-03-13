using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class ProjectAllocation
    {
        public Guid Id { get; set; }

         public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

         public Guid UserId { get; set; }
        public NormalUser User { get; set; } = null!;

         public AllocationType AllocationType { get; set; } = AllocationType.Resource;

         public decimal AllocatedHours { get; set; } = 0m;
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

         public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}