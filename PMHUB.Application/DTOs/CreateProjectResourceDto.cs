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
        [Required(ErrorMessage = "Le nom de la ressource est obligatoire.")]
        [MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal PricePerUnit { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        [MaxLength(100)]
        public string? CostCenter { get; set; }
    }
}
