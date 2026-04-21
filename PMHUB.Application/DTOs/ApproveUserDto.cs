 
using System.ComponentModel.DataAnnotations;
 

namespace PMHUB.Application.DTOs
{
    public class ApproveUserDto
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Approval decision is required.")]
        public bool IsApproved { get; set; }
    }
}
