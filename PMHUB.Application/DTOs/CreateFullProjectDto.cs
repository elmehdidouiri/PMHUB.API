using PMHUB.Application.DTOs;
using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public class CreateFullProjectDto
{
    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Le département est obligatoire.")]
    public Guid DepartmentId { get; set; }

    [Required(ErrorMessage = "Le budget est obligatoire.")]
    [Range(0, double.MaxValue)]
    public decimal Budget { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire.")]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
    public DateTime? EstimatedDueDate { get; set; }

    public ProjectPhase Phase { get; set; } = ProjectPhase.Pipeline;
    public ProjectStatus Status { get; set; } = ProjectStatus.Ongoing;
    public ProcessStatus ProcessStatus { get; set; } = ProcessStatus.NotStarted;

    [MaxLength(150)]
    public string? ProjectManager { get; set; }

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

    [Required(ErrorMessage = "Le type de projet est obligatoire.")]
    public ProjectManagementType ProjectManagementType { get; set; }

    [MaxLength(1000)]
    public string? CurrentState { get; set; }

    [MaxLength(1000)]
    public string? Roadblocks { get; set; }

    [MaxLength(1000)]
    public string? NextSteps { get; set; }

    [MaxLength(1000)]
    public string? Enhancements { get; set; }

    public decimal EstimatedHours { get; set; } = 0;
 
    public decimal StrategicScore { get; set; } = 0;

    public Guid? ParentProjectId { get; set; }

     public ICollection<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public ICollection<Guid> TechnologyIds { get; set; } = new List<Guid>();
    public ICollection<Guid> SolutionDomainIds { get; set; } = new List<Guid>();
    public ICollection<Guid> MemberIds { get; set; } = new List<Guid>();

     public ICollection<CreateSubProjectDto> SubProjects { get; set; } = new List<CreateSubProjectDto>();
    public ICollection<CreateProjectResourceDto> ProjectResources { get; set; } = new List<CreateProjectResourceDto>();
    public ICollection<CreateStrategicCriterionDto> StrategicCriteria { get; set; } = new List<CreateStrategicCriterionDto>();
    public ICollection<CreateProjectAllocationDto> Allocations { get; set; } = new List<CreateProjectAllocationDto>();
    public ICollection<CreateInternAllocationDto> InternAllocations { get; set; } = new List<CreateInternAllocationDto>();
    public ICollection<CreateKpiDto> KPIs { get; set; } = new List<CreateKpiDto>();

}
