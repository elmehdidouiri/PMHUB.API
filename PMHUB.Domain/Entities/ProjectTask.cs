using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class ProjectTask
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TaskStatuss Status { get; set; } = TaskStatuss.NotStarted;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

         [Required]
        public Guid ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

         public Guid? SprintId { get; set; }
        [ForeignKey("SprintId")]
        public Sprint? Sprint { get; set; }

         public Guid? AssignedUserId { get; set; }
        [ForeignKey("AssignedUserId")]
        public NormalUser? AssignedUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

     }
}