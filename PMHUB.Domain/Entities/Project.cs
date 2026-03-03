using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PMHUB.Domain.Enums;

namespace PMHUB.Domain.Entities
{
    public class Project
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public ProjectStatus Status { get; set; }

        [Required]
        public ProjectPhase Phase { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

         public Guid DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

         public Guid? ParentProjectId { get; set; }
        [ForeignKey("ParentProjectId")]
        public Project? ParentProject { get; set; }
        public ICollection<Project> SubProjects { get; set; } = new List<Project>();

         public ICollection<ProjectBusinessUnit> ProjectBusinessUnits { get; set; } = new List<ProjectBusinessUnit>();
        public ICollection<ProjectTechnology> ProjectTechnologies { get; set; } = new List<ProjectTechnology>();
        public ICollection<ProjectSolutionDomain> ProjectSolutionDomains { get; set; } = new List<ProjectSolutionDomain>();
        public ICollection<User> Members { get; set; } = new List<User>();
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        public ICollection<ProjectAllocation> ProjectAllocations { get; set; } = new List<ProjectAllocation>();
        public ICollection<InternAllocation> InternAllocations { get; set; } = new List<InternAllocation>();
        public ICollection<HourEntry> HourEntries { get; set; } = new List<HourEntry>();
        public ICollection<KPI> KPIs { get; set; } = new List<KPI>();
        public ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();
        public ICollection<ProjectResource> ProjectResources { get; set; } = new List<ProjectResource>();
        public ICollection<StrategicCriterion> StrategicCriteria { get; set; } = new List<StrategicCriterion>();
    }
}