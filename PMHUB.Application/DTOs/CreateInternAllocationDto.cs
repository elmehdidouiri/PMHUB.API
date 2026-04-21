using System;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CreateInternAllocationDto
    {
        [Required(ErrorMessage = "Intern ID is required.")]
        public Guid InternId { get; set; }

        [Required(ErrorMessage = "Allocated hours are required.")]
        [Range(0.1, 1000, ErrorMessage = "Allocated hours must be between 0.1 and 1000.")]
        public decimal AllocatedHours { get; set; }
    }
}