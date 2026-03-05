using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.DTOs
{
    public class CreateKpiDto
    {
        [Required(ErrorMessage = "Le nom du KPI est obligatoire.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; 

        [Range(0, double.MaxValue)]
        public decimal TargetValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal CurrentValue { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
