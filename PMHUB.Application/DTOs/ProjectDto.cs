using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ProjectStatus Status { get; set; }
        public string StatusLabel => Status.ToString();

        public ProjectPhase Phase { get; set; }
        public string PhaseLabel => Phase.ToString();

        public ProcessStatus ProcessStatus { get; set; }
        public string ProcessStatusLabel => ProcessStatus.ToString();

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EstimatedDueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public decimal Budget { get; set; }
        public decimal DigitalContribution { get; set; }
        public decimal CostSaving { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal StrategicScore { get; set; }
        public int ProgressPercentage { get; set; }

        public string? ProjectManager { get; set; }
        public string? Sponsor { get; set; }
        public string? CostCenter { get; set; }
        public string? CodeSourceLink { get; set; }
        public string? SolutionLink { get; set; }
        public string? ServerHostName { get; set; }
        public ProjectManagementType ProjectManagementType { get; set; }
        public string ProjectManagementTypeLabel => ProjectManagementType.ToString();
        public string? CurrentState { get; set; }
        public string? Roadblocks { get; set; }
        public string? NextSteps { get; set; }
        public string? Enhancements { get; set; }

         public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string BusinessUnitName { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;

         public Guid? ParentProjectId { get; set; }
        public string? ParentProjectName { get; set; }

         public ICollection<string> BusinessUnits { get; set; } = new List<string>();
        public ICollection<string> Technologies { get; set; } = new List<string>();
        public ICollection<string> SolutionDomains { get; set; } = new List<string>();
        public ICollection<string> Members { get; set; } = new List<string>();

         public ICollection<KpiDto> KPIs { get; set; } = new List<KpiDto>();

         public ICollection<ProjectResourceDto> ProjectResources { get; set; } = new List<ProjectResourceDto>();
        public ICollection<ProjectSummaryDto> SubProjects { get; set; } = new List<ProjectSummaryDto>();
    }
}