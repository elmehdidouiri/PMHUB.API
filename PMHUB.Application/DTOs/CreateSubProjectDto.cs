using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CreateSubProjectDto
    {
        [Required(ErrorMessage = "Le nom du sous-projet est obligatoire.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le budget est obligatoire.")]
        [Range(0, double.MaxValue, ErrorMessage = "Le budget doit être positif.")]
        public decimal Budget { get; set; }

        [Required(ErrorMessage = "La date de début est obligatoire.")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ProjectPhase Phase { get; set; } = ProjectPhase.Pipeline;
        public ProjectStatus Status { get; set; } = ProjectStatus.Ongoing;
    }
}