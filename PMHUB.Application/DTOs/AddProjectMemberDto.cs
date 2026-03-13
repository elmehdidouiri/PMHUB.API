using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class AddProjectMemberDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}
