using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class UpdateProjectDto
    {
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(150, ErrorMessage = "Le nom ne peut pas dépasser 150 caractères.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
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
        public ProjectPhase Phase { get; set; }

        [Required(ErrorMessage = "Le statut est obligatoire.")]
        public ProjectStatus Status { get; set; }

        public Guid? ParentProjectId { get; set; }

        // Remplacement complet des listes Many-to-Many
        public ICollection<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
        public ICollection<Guid> TechnologyIds { get; set; } = new List<Guid>();
        public ICollection<Guid> SolutionDomainIds { get; set; } = new List<Guid>();
        public ICollection<Guid> MemberIds { get; set; } = new List<Guid>();
    }

}
