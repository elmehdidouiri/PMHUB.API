using System.ComponentModel.DataAnnotations;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.DTOs
{
    public class UpdateProjectDto
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150, ErrorMessage = "Le nom ne peut pas dépasser 150 caractères.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le département est obligatoire.")]
        public Guid DepartmentId { get; set; }

        [Required(ErrorMessage = "Le budget est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le budget doit être positif.")]
        public decimal Budget { get; set; }

        [Required(ErrorMessage = "La date de début est obligatoire.")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
        public DateTime? EstimatedDueDate { get; set; }

        [Required(ErrorMessage = "La phase est obligatoire.")]
        public ProjectPhase Phase { get; set; }

        [Required(ErrorMessage = "Le statut est obligatoire.")]
        public ProjectStatus Status { get; set; }

        public ProcessStatus ProcessStatus { get; set; } = ProcessStatus.NotStarted;

        public Guid? ParentProjectId { get; set; }

        // Informations projet
        [MaxLength(150)]
        public string? ProjectManager { get; set; }

        [MaxLength(150)]
        public string? Sponsor { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "La contribution digitale doit être positive.")]
        public decimal DigitalContribution { get; set; } = 0;

        [MaxLength(100)]
        public string? CostCenter { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le coût économisé doit être positif.")]
        public decimal CostSaving { get; set; } = 0;

        [Range(0, 100, ErrorMessage = "Le pourcentage doit être entre 0 et 100.")]
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

        [Range(0, double.MaxValue, ErrorMessage = "Les heures estimées doivent être positives.")]
        public decimal EstimatedHours { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Le score stratégique doit être positif.")]
        public decimal StrategicScore { get; set; } = 0;

         public ICollection<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
        public ICollection<Guid> TechnologyIds { get; set; } = new List<Guid>();
        public ICollection<Guid> SolutionDomainIds { get; set; } = new List<Guid>();
        public ICollection<Guid> MemberIds { get; set; } = new List<Guid>();
    }
}