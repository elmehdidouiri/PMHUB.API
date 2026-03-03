using PMHUB.Application.DTOs;
using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public class CreateFullProjectDto
{
    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(150, ErrorMessage = "Le nom ne peut pas dépasser 150 caractères.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Le département est obligatoire.")]
    public Guid DepartmentId { get; set; }

    [Required(ErrorMessage = "Le budget est obligatoire.")]
    [Range(0, double.MaxValue, ErrorMessage = "Le budget doit être positif.")]
    public decimal Budget { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire.")]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = "La phase est obligatoire.")]
    public ProjectPhase Phase { get; set; } = ProjectPhase.Pipeline;

    [Required(ErrorMessage = "Le statut est obligatoire.")]
    public ProjectStatus Status { get; set; } = ProjectStatus.Ongoing;

     public Guid? ParentProjectId { get; set; }

     public ICollection<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public ICollection<Guid> TechnologyIds { get; set; } = new List<Guid>();
    public ICollection<Guid> SolutionDomainIds { get; set; } = new List<Guid>();
    public ICollection<Guid> MemberIds { get; set; } = new List<Guid>();

     public ICollection<CreateSubProjectDto> SubProjects { get; set; } = new List<CreateSubProjectDto>();
    public ICollection<CreateProjectAllocationDto> Allocations { get; set; } = new List<CreateProjectAllocationDto>();
    public ICollection<CreateInternAllocationDto> InternAllocations { get; set; } = new List<CreateInternAllocationDto>();
    public ICollection<CreateProjectResourceDto> ProjectResources { get; set; } = new List<CreateProjectResourceDto>();
    public ICollection<CreateStrategicCriterionDto> StrategicCriteria { get; set; } = new List<CreateStrategicCriterionDto>();
}
