using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateProjectResourceDto
    {
        [Required(ErrorMessage = "Resource name is required.")]
        [MaxLength(150, ErrorMessage = "Resource name cannot exceed 150 characters.")]
        public string ItemName { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price per unit must be a positive value.")]
        public decimal PricePerUnit { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive value.")]
        public int Quantity { get; set; } = 0;

        [MaxLength(100)]
        public string? CostCenter { get; set; }
    }
}
