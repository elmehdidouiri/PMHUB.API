using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Role name is required.")]
        [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}
