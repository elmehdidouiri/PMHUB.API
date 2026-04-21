using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class UpdateDepartmentDto
    {
        [Required(ErrorMessage = "Department name is required.")]
        [MaxLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business Unit is required.")]
        public Guid BusinessUnitId { get; set; }

        [Required(ErrorMessage = "Plant is required.")]
        public Guid PlantId { get; set; }
    }
}
