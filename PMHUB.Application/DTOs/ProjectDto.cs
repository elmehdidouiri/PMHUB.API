using PMHUB.Domain.Enums;
using System;

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
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Budget { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

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

        // Sous-projets
        public ICollection<ProjectSummaryDto> SubProjects { get; set; } = new List<ProjectSummaryDto>();
    }

}