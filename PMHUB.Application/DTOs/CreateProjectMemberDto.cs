using System.ComponentModel.DataAnnotations;
 

namespace PMHUB.Application.DTOs
{
    public class CreateProjectMemberDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Role ID is required.")]
        public Guid RoleId { get; set; }

        public string? Role { get; set; }
     }
}
