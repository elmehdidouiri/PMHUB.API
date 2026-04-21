using System.ComponentModel.DataAnnotations;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    public class UpdateProjectDto 
    {
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(150, ErrorMessage = "Project name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public Guid? DepartmentId { get; set; }

        [Required(ErrorMessage = "Budget is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive value.")]
        public decimal? Budget { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime? StartDate { get; set; }
        public Guid? ProjectManagerId { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EstimatedDueDate { get; set; }

        [Required(ErrorMessage = "Project phase is required.")]
        public ProjectPhase? Phase { get; set; }

        [Required(ErrorMessage = "Project status is required.")]
        public ProjectStatus? Status { get; set; }

        public ProcessStatus ProcessStatus { get; set; } = ProcessStatus.NotStarted;

        [Required(ErrorMessage = "Project management type is required.")]
        public ProjectManagementType? ProjectManagementType { get; set; }

        [Required(ErrorMessage = "Project type is required.")]
        public ProjectType? ProjectType { get; set; }

        public Guid? ParentProjectId { get; set; }

        [MaxLength(150)]
        public string? Sponsor { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Digital contribution must be a positive value.")]
        public decimal DigitalContribution { get; set; } = 0;

        [MaxLength(100)]
        public string? CostCenter { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost saving must be a positive value.")]
        public decimal CostSaving { get; set; } = 0;

        [Range(0, 100, ErrorMessage = "Progress percentage must be between 0 and 100.")]
        public int ProgressPercentage { get; set; } = 0;

        [MaxLength(500)]
        public string? CodeSourceLink { get; set; }

        [MaxLength(500)]
        public string? SolutionLink { get; set; }

        [MaxLength(150)]
        public string? ServerHostName { get; set; }

        [MaxLength(1000)]
        public string? CurrentState { get; set; }

        [MaxLength(1000)]
        public string? NextSteps { get; set; }

        [MaxLength(1000)]
        public string? Enhancements { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Estimated hours must be a positive value.")]
        public decimal EstimatedHours { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Actual hours must be a positive value.")]
        public decimal ActualHours { get; set; } = 0;

        public ICollection<CreateStrategicCriterionDto> StrategicCriteria { get; set; }
            = new List<CreateStrategicCriterionDto>();

         public ICollection<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
        public ICollection<Guid> TechnologyIds { get; set; } = new List<Guid>();
        public ICollection<Guid> SolutionDomainIds { get; set; } = new List<Guid>();
        public ICollection<CreateProjectMemberDto> Members { get; set; }
              = new List<CreateProjectMemberDto>();
    }
}