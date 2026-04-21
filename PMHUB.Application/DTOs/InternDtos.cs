using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class InternDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public Guid SupervisorId { get; set; }
        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateInternDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role ID is required.")]
        public Guid RoleId { get; set; }

        [Required(ErrorMessage = "Supervisor ID is required.")]
        public Guid SupervisorId { get; set; }
    }

    public class UpdateInternDto
    {
        [Required(ErrorMessage = "ID is required.")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role ID is required.")]
        public Guid RoleId { get; set; }

        [Required(ErrorMessage = "Supervisor ID is required.")]
        public Guid SupervisorId { get; set; }
    }
}
