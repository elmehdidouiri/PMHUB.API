 
using System.ComponentModel.DataAnnotations;
 

namespace PMHUB.Application.DTOs
{
    public class CreateBusinessUnitDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }
    }
}
