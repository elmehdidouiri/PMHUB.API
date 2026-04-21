using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class ProjectInternAllocationDto
    {
        public Guid InternAllocationId { get; set; }
        public Guid InternId { get; set; }
        public string InternName { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public Guid SupervisorId { get; set; }
        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;
        public decimal AllocatedHours { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal RemainingHours { get; set; }
        public DateTime AllocationDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<InternHourEntryDto> HourEntries { get; set; } = new List<InternHourEntryDto>();
    }

    public class InternHourEntryDto
    {
        public Guid Id { get; set; }
        public Guid InternAllocationId { get; set; }
        public Guid BookedByUserId { get; set; }
        public string BookedByUserName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateProjectInternAllocationDto
    {
        [Required(ErrorMessage = "Intern ID is required.")]
        public Guid InternId { get; set; }

        [Required(ErrorMessage = "Allocated hours are required.")]
        [Range(0.1, 1000, ErrorMessage = "Allocated hours must be between 0.1 and 1000.")]
        public decimal AllocatedHours { get; set; }

        public DateTime? AllocationDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateProjectInternAllocationDto
    {
        [Required(ErrorMessage = "Allocated hours are required.")]
        [Range(0.1, 1000, ErrorMessage = "Allocated hours must be between 0.1 and 1000.")]
        public decimal AllocatedHours { get; set; }

        [Required(ErrorMessage = "Allocation date is required.")]
        public DateTime AllocationDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class CreateInternHourEntryDto
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Hours are required.")]
        [Range(0.1, 24, ErrorMessage = "Hours must be between 0.1 and 24.")]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateInternHourEntryDto
    {
        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Hours are required.")]
        [Range(0.1, 24, ErrorMessage = "Hours must be between 0.1 and 24.")]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
