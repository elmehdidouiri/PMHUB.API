using System;
using System.ComponentModel.DataAnnotations;

namespace PMHUB.Application.DTOs
{
    public class CreateInternAllocationDto
    {
        [Required]
        public Guid InternId { get; set; }

        [Required]
        [Range(0.1, 1000, ErrorMessage = "Allocated hours must be greater than 0")]
        public decimal AllocatedHours { get; set; }
    }
}