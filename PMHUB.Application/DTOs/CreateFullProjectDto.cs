using PMHUB.Application.DTOs;
using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class CreateFullProjectDto
{
    [Required(ErrorMessage = "Project name is required.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public Guid? DepartmentId { get; set; }
    public ICollection<Guid> DepartmentIds { get; set; } = new List<Guid>();

    [Range(0, double.MaxValue)]
    public decimal? Budget { get; set; }

    public DateTime? StartDate { get; set; }
    public Guid? ProjectManagerId { get; set; }

    public DateTime? EndDate { get; set; }
    public DateTime? EstimatedDueDate { get; set; }

    public ProjectPhase Phase { get; set; } = ProjectPhase.Pipeline;
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
    public ProcessStatus ProcessStatus { get; set; } = ProcessStatus.NotStarted;

 

    [MaxLength(150)]
    public string? Sponsor { get; set; }

    public decimal DigitalContribution { get; set; } = 0;

    [MaxLength(100)]
    public string? CostCenter { get; set; }

    public decimal CostSaving { get; set; } = 0;
    public int ProgressPercentage { get; set; } = 0;

    [MaxLength(500)]
    public string? CodeSourceLink { get; set; }

    [MaxLength(500)]
    public string? SolutionLink { get; set; }

    [MaxLength(150)]
    public string? ServerHostName { get; set; }

    [Required(ErrorMessage = "Project management type is required.")]
    public Category? ProjectManagementType { get; set; }

    [Required(ErrorMessage = "Project type is required.")]
    public ProjectType? ProjectType { get; set; }

    [MaxLength(1000)]
    public string? CurrentState { get; set; }

    [MaxLength(1000)]
    public string? NextSteps { get; set; }

    [MaxLength(1000)]
    public string? Enhancements { get; set; }

    public decimal EstimatedHours { get; set; } = 0;
    public decimal ActualHours { get; set; } = 0;
 
 
    public Guid? ParentProjectId { get; set; }

     public ICollection<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public ICollection<Guid> TechnologyIds { get; set; } = new List<Guid>();
    public ICollection<Guid> SolutionDomainIds { get; set; } = new List<Guid>();
    public ICollection<CreateProjectMemberDto> Members { get; set; }
        = new List<CreateProjectMemberDto>();

    [JsonPropertyName("teamMembers")]
    public ICollection<CreateProjectMemberDto> TeamMembers { get; set; }
        = new List<CreateProjectMemberDto>();

    public ICollection<CreateProjectResourceDto> ProjectResources { get; set; } = new List<CreateProjectResourceDto>();
    public ICollection<CreateStrategicCriterionDto> StrategicCriteria { get; set; } = new List<CreateStrategicCriterionDto>();
     public ICollection<CreateKpiDto> KPIs { get; set; } = new List<CreateKpiDto>();

}
