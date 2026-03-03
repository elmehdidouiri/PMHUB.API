using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class ProjectTask
    {
          public Guid Id { get; set; }

             public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public TaskStatuss Status { get; set; } = TaskStatuss.NotStarted;

             public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

             public Guid ProjectId { get; set; }
            public Project Project { get; set; } = null!;

             public Guid? AssignedUserId { get; set; }
            public User? AssignedUser { get; set; }

             public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
}