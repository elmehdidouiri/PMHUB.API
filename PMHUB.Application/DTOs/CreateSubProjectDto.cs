using PMHUB.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CreateSubProjectDto
    {
        [Required(ErrorMessage = "Sub-project name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Budget is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive value.")]
        public decimal Budget { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ProjectPhase Phase { get; set; } = ProjectPhase.Pipeline;
        public ProjectStatus Status { get; set; } = ProjectStatus.Planned;
    }
}
