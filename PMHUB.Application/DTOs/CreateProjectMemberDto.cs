using System.ComponentModel.DataAnnotations;
 

namespace PMHUB.Application.DTOs
{
    public class CreateProjectMemberDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }
     }
}
