using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;
using PMHUB.Domain.Entities;

namespace PMHUB.Domain.Entities
{
    public class HourEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

         public Guid? UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public Guid? InternAllocationId { get; set; }
        [ForeignKey("InternAllocationId")]
        public InternAllocation? InternAllocation { get; set; }

         [Required]
        public Guid ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

         public Guid? TaskId { get; set; }
        [ForeignKey("TaskId")]
        public ProjectTask? Task { get; set; }

         public Guid? SprintId { get; set; }
        [ForeignKey("SprintId")]
        public Sprint? Sprint { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Hours { get; set; }

        [Required]
        public AllocationType AllocationType { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}